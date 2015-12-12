﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using arma3Launcher.Workers;
using arma3Launcher.Controls;
using System.Threading;
using CG.Web.MegaApiClient;
using System.Diagnostics;
using System.IO;

namespace arma3Launcher.Workers
{
    class Downloader
    {
        // controls
        private Windows7ProgressBar progressFile;
        private Windows7ProgressBar progressAll;
        private Label progressCurFile;
        private Label progressText;
        private Label progressDetails;
        private PictureBox launcherButton;
        private MegaApiClient megaClient;
        private Installer installer;

        // forms
        private MainForm mainForm;

        // background workers
        private BackgroundWorker calculateFiles = new BackgroundWorker();
        private BackgroundWorker downloadFiles = new BackgroundWorker();
        
        // download stuff
        private Queue<string> downloadUrls = new Queue<string>();
        private IEnumerable<string> listUrls = new List<string>();

        // folder paths
        private string TempFolder = Path.GetTempPath() + @"arma3Launcher\";

        // paramters
        private bool isLaunch = false;
        private string configUrl = "";
        private string activePack = "";

        // controllers
        private bool downloadRunning = false;
        private int totalDownloads = 0;
        private int parsedDownloads = 0;
        private Int64 parsedBytes;
        private Int64 totalBytes;

        // error report
        private EmailReporter reportError;

        // converter
        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024) / 1024;
        }

        // callbacks
        delegate void stringCallBack(string text);
        delegate void intCallBack(int number);

        // invokes
        private void progressBarFileStyle(ProgressBarStyle prbStyle)
        {
            if (this.progressFile.InvokeRequired)
            {
                this.progressFile.Invoke(new MethodInvoker(delegate { this.progressFile.Style = prbStyle; }));
            }
            else
            {
                this.progressFile.Style = prbStyle;
            }
        }

        private void progressBarFileState(ProgressBarState prbState)
        {
            if (this.progressFile.InvokeRequired)
            {
                this.progressFile.Invoke(new MethodInvoker(delegate { this.progressFile.State = prbState; }));
            }
            else
            {
                this.progressFile.State = prbState;
            }
        }

        private void currentFileText(string text)
        {
            if (this.progressCurFile.InvokeRequired)
            {
                stringCallBack d = new stringCallBack(currentFileText);
                this.mainForm.Invoke(d, new object[] { text });
            }
            else
            {
                this.progressCurFile.Text = text;
            }
        }

        private void progressStatusText(string text)
        {
            if (this.progressText.InvokeRequired)
            {
                stringCallBack d = new stringCallBack(progressStatusText);
                this.mainForm.Invoke(d, new object[] { text });
            }
            else
            {
                this.progressText.Text = text;
            }
        }

        private void progressDetailsText(string text)
        {
            if (this.progressDetails.InvokeRequired)
            {
                stringCallBack d = new stringCallBack(progressDetailsText);
                this.mainForm.Invoke(d, new object[] { text });
            }
            else
            {
                this.progressDetails.Text = text;
            }
        }

        private void progressBarFileValue(int prbValue)
        {
            if (this.progressFile.InvokeRequired)
            {
                intCallBack d = new intCallBack(progressBarFileValue);
                this.mainForm.Invoke(d, new object[] { prbValue });
            }
            else
            {
                this.progressFile.Value = prbValue;
            }
        }

        private void progressBarAllValue(int prbValue)
        {
            if (this.progressAll.InvokeRequired)
            {
                intCallBack d = new intCallBack(progressBarAllValue);
                this.mainForm.Invoke(d, new object[] { prbValue });
            }
            else
            {
                this.progressAll.Value = prbValue;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mainForm"></param>
        /// <param name="progressFile"></param>
        /// <param name="progressAll"></param>
        /// <param name="progressText"></param>
        /// <param name="progressDetails"></param>
        /// <param name="launcherButton"></param>
        public Downloader (MainForm mainForm, Installer installerWorker, Windows7ProgressBar progressFile, Windows7ProgressBar progressAll, Label progressCurFile, Label progressText, Label progressDetails, PictureBox launcherButton)
        {
            this.mainForm = mainForm;
            this.installer = installerWorker;
            this.megaClient = new MegaApiClient();

            // construct error report
            this.reportError = new EmailReporter();

            // define controls
            this.progressCurFile = progressCurFile;
            this.progressFile = progressFile;
            this.progressAll = progressAll;
            this.progressText = progressText;
            this.progressDetails = progressDetails;
            this.launcherButton = launcherButton;

            // define calculate worker
            this.calculateFiles.DoWork += CalculateFiles_DoWork;
            this.calculateFiles.RunWorkerCompleted += CalculateFiles_RunWorkerCompleted;

            // define download worker
            this.downloadFiles.DoWork += DownloadFiles_DoWork;
            this.downloadFiles.RunWorkerCompleted += DownloadFiles_RunWorkerCompleted;
        }

        private void CalculateFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            // reset variables
            this.parsedBytes = 0;
            this.totalBytes = 0;

            foreach (var url in listUrls)
            {
                using (Stream webStream = megaClient.Download(new Uri(url)))
                    this.totalBytes += Convert.ToInt64(webStream.Length);
            }
        }

        private void CalculateFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.downloadFiles.RunWorkerAsync();
        }

        /// <summary>
        /// Download background worker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            // specify the URL of the file to download
            string url = this.downloadUrls.Peek();

            // specify the output file name
            string outputFile = url.Split('!')[1] + ".zip";

            // create output directory (if necessary)
            string outputFolder = this.TempFolder;
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            // auxiliary variables
            string outputComplete = outputFolder + outputFile;
            string downloadSpeed = "";
            int progressPercentage = 0;


            // download the file and write it to disk
            using (Stream webStream = megaClient.Download(new Uri(url)))
            using (FileStream fileStream = new FileStream(outputComplete, FileMode.Create))
            {
                var buffer = new byte[32768];
                int bytesRead;
                Int64 bytesReadComplete = 0;  // use Int64 for files larger than 2 gb

                // get the size of the file to download
                Int64 bytesTotal = Convert.ToInt64(webStream.Length);

                // start a new StartWatch for measuring download time
                Stopwatch sw = Stopwatch.StartNew();

                // download file in chunks
                while ((bytesRead = webStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bytesReadComplete += bytesRead;
                    parsedBytes += bytesRead;
                    fileStream.Write(buffer, 0, bytesRead);

                    if ((bytesReadComplete / 1024d / sw.Elapsed.TotalSeconds) > 999)
                        downloadSpeed = String.Format("{0:F1} mb/s", bytesReadComplete / 1048576d / sw.Elapsed.TotalSeconds);
                    else
                        downloadSpeed = String.Format("{0:F1} kb/s", bytesReadComplete / 1024d / sw.Elapsed.TotalSeconds);

                    progressPercentage = Convert.ToInt32(((double)bytesReadComplete / bytesTotal) * 100);

                    this.progressBarFileStyle(ProgressBarStyle.Continuous);
                    this.progressBarFileValue(progressPercentage);
                    this.progressBarAllValue(Convert.ToInt32(((double)parsedBytes / totalBytes) * 100));
                    this.progressStatusText(String.Format("Downloading ({0:F0}/{1:F0}) {2}... {3:F0}%", this.parsedDownloads, this.totalDownloads, outputFile, progressPercentage));
                    this.progressDetailsText(String.Format("{0:0}MB of {1:0}MB / {2}", ConvertBytesToMegabytes(bytesReadComplete), ConvertBytesToMegabytes(bytesTotal), downloadSpeed));
                }

                sw.Stop();
            }
        }

        private void DownloadFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.downloadUrls.Dequeue();
            this.SaveDownloadQueue();

            if (this.parsedDownloads < this.totalDownloads)
                this.parsedDownloads++;

            if (this.downloadUrls.Count > 0)
                this.downloadFiles.RunWorkerAsync();
            else
            {
                this.progressDetailsText("");
                this.currentFileText("");
                this.downloadRunning = false;
                this.megaClient.Logout();
                installer.beginInstall(this.isLaunch, this.configUrl, this.activePack);
            }
        }

        /// <summary>
        /// Saves download queue to allow download resume after crash or failure
        /// </summary>
        public void SaveDownloadQueue()
        {
            if (this.downloadUrls.Count != 0)
            {
                string aux_downloadQueue = "";
                foreach (var item in this.downloadUrls)
                {
                    if (aux_downloadQueue == "")
                        aux_downloadQueue = item + ",";
                    else
                        aux_downloadQueue = aux_downloadQueue + item + ",";
                }
                Properties.Settings.Default.downloadQueue = aux_downloadQueue;
            }
            else
            { Properties.Settings.Default.downloadQueue = ""; }

            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Checks if there are downloads running
        /// </summary>
        /// <returns> downloadRunning </returns>
        public bool isDownloading()
        {
            return this.downloadRunning;
        }

        /// <summary>
        /// Enqueue link into an already active download queue
        /// </summary>
        /// <param name="urlLink"></param>
        public void enqueueUrl(string urlLink)
        {
            this.downloadUrls.Enqueue(urlLink);
            this.totalDownloads++;
        }

        /// <summary>
        /// Begins the process to download
        /// </summary>
        /// <param name="urlsList"></param>
        /// <param name="isConfig"></param>
        public void beginDownload(IEnumerable<string> listUrls, bool isLaunch, string activePack, string configUrl)
        {
            // lock controls
            this.launcherButton.Enabled = false;

            // report status
            this.progressStatusText("Connecting to the host...");
            this.currentFileText("Download server: MEGA (mega.nz)");
            this.progressBarFileStyle(ProgressBarStyle.Marquee);

            // define paramters
            this.isLaunch = isLaunch;
            this.activePack = activePack;
            this.configUrl = configUrl;

            // fill urls list
            this.listUrls = listUrls;

            // define urls
            foreach (var url in listUrls)
            {
                this.downloadUrls.Enqueue(url);
            }

            // restart counters
            this.totalDownloads = downloadUrls.Count;
            this.parsedDownloads = 1;

            // begin download
            this.downloadRunning = true;
            this.megaClient.LoginAnonymous();
            this.calculateFiles.RunWorkerAsync();
        }
    }
}
