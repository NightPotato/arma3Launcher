﻿using arma3Launcher.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using arma3Launcher.Workers;
using MaterialSkin.Controls;
using MaterialSkin;

namespace arma3Launcher.Workers
{
    class Installer
    {
        // controls
        private readonly Windows7ProgressBar progressFile;
        private readonly Windows7ProgressBar progressAll;
        private readonly Label progressText;
        private readonly Label progressDetails;
        private readonly Label progressCurFile;
        private DoubleBufferFlowPanel flowpanelAddonPacks;
        private readonly PictureBox cancelButton;
        private readonly String activeForm;
        private readonly MaterialFlatButton repoValidateBtn;

        // controls (directory fields)
        private readonly MaterialSingleLineTextField gamePathBox;
        private readonly MaterialSingleLineTextField ts3PathBox;
        private readonly MaterialSingleLineTextField addonsPathBox;
        private readonly PictureBox gamePathErase;
        private readonly PictureBox ts3PathErase;
        private readonly PictureBox addonsPathErase;
        private readonly PictureBox gamePathFind;
        private readonly PictureBox ts3PathFind;
        private readonly PictureBox addonsPathFind;

        // workers
        private RepoReader repoReader = new RepoReader();

        // controls (toolstrip menu items)
        private readonly MaterialRaisedButton ts3Plugin;

        // forms
        private MainForm2 mainForm;

        // background workers
        private readonly BackgroundWorker installFiles = new BackgroundWorker();
        private readonly BackgroundWorker validateFiles = new BackgroundWorker();

        // timer
        private readonly Timer delayLaunch = new Timer();

        // folder paths
        private readonly string TempFolder = Path.GetTempPath() + @"arma3Launcher\";
        private string GameFolder = string.Empty;
        private string TS3Folder = string.Empty;
        private string AddonsFolder = string.Empty;

        // paramters
        private bool isLaunch = false;

        // controllers
        private bool installationRunning = false;
        private bool isTFR = false;
        private bool isInstall = false;

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
                this.progressCurFile.Invoke(new MethodInvoker(delegate { this.progressCurFile.Text = text; }));
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
                this.progressText.Invoke(new MethodInvoker(delegate { this.progressText.Text = text; }));
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
                this.progressDetails.Invoke(new MethodInvoker(delegate { this.progressDetails.Text = text; }));
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
                this.progressFile.Invoke(new MethodInvoker(delegate { this.progressFile.Value = prbValue; }));
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
                this.progressAll.Invoke(new MethodInvoker(delegate { this.progressAll.Value = prbValue; }));
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
        /// <param name="progressCurFile"></param>
        /// <param name="launcherButton"></param>
        public Installer (MainForm2 mainForm, Windows7ProgressBar progressFile, Windows7ProgressBar progressAll, Label progressText, Label progressDetails, Label progressCurFile, DoubleBufferFlowPanel flowpanelAddonPacks, PictureBox cancelButton,
            MaterialSingleLineTextField gamePathBox, MaterialSingleLineTextField ts3PathBox, MaterialSingleLineTextField addonsPathBox, PictureBox gamePathErase, PictureBox ts3PathErase, PictureBox addonsPathErase, PictureBox gamePathFind, PictureBox ts3PathFind, PictureBox addonsPathFind,
            MaterialRaisedButton ts3Plugin, MaterialFlatButton repoValidateBtn)
        {
            this.activeForm = "mainForm";
            this.mainForm = mainForm;

            // define controls
            this.progressFile = progressFile;
            this.progressAll = progressAll;
            this.progressText = progressText;
            this.progressDetails = progressDetails;
            this.progressCurFile = progressCurFile;
            this.flowpanelAddonPacks = flowpanelAddonPacks;
            this.cancelButton = cancelButton;
            this.repoValidateBtn = repoValidateBtn;

            // define controls (directory fields)
            this.gamePathBox = gamePathBox;
            this.gamePathErase = gamePathErase;
            this.gamePathFind = gamePathFind;

            this.ts3PathBox = ts3PathBox;
            this.ts3PathErase = ts3PathErase;
            this.ts3PathFind = ts3PathFind;

            this.addonsPathBox = addonsPathBox;
            this.addonsPathErase = addonsPathErase;
            this.addonsPathFind = addonsPathFind;

            // define controls (toolstrip menu items)
            this.ts3Plugin = ts3Plugin;

            // define background worker
            this.installFiles.DoWork += InstallFiles_DoWork;
            this.installFiles.RunWorkerCompleted += InstallFiles_RunWorkerCompleted;

            this.validateFiles.DoWork += ValidateFiles_DoWork;
            this.validateFiles.RunWorkerCompleted += ValidateFiles_RunWorkerCompleted;

            // define timer
            this.delayLaunch.Interval = 2000;
            this.delayLaunch.Tick += DelayLaunch_Tick;
        }

        /// <summary>
        /// Begins the process of installation
        /// </summary>
        /// <param name="isConfig"></param>
        public void beginInstall(bool isLaunch, bool isTFR)
        {
            // report status
            this.progressStatusText("Preparing the installation process...");

            // reset progress bars
            this.progressFile.Value = 0;
            this.progressAll.Value = 0;

            // define if is launch
            this.isLaunch = isLaunch;

            // define if is TFR
            this.isTFR = isTFR;

            // define paths
            this.GameFolder = Properties.Settings.Default.Arma3Folder;
            this.TS3Folder = Properties.Settings.Default.TS3Folder;
            this.AddonsFolder = Properties.Settings.Default.AddonsFolder;

            // lock directory fields
            this.gamePathBox.Enabled = false;
            this.gamePathErase.Enabled = false;
            this.gamePathFind.Enabled = false;

            this.ts3PathBox.Enabled = false;
            this.ts3PathErase.Enabled = false;
            this.ts3PathFind.Enabled = false;

            this.addonsPathBox.Enabled = false;
            this.addonsPathErase.Enabled = false;
            this.addonsPathFind.Enabled = false;

            // hide cancel button
            try { this.cancelButton.Enabled = false; this.cancelButton.Image = Properties.Resources.cancel_circle_big_inactive; } catch { }

            // begin installation
            this.isInstall = true;
            this.installationRunning = true;
            GlobalVar.isInstalling = true;

            if(repoReader.IsRepoDifferent(repoReader.GetRepoFile()))
                this.validateFiles.RunWorkerAsync();
            else
                this.installFiles.RunWorkerAsync();
        }

        public void ValidateLocalFiles()
        {
            // show download panel
            this.mainForm.ShowDownloadPanel();

            // define paths
            this.AddonsFolder = Properties.Settings.Default.AddonsFolder;

            // hide cancel button
            try { this.cancelButton.Enabled = false; this.cancelButton.Image = Properties.Resources.cancel_circle_big_inactive; } catch { }

            // begin validation
            this.isInstall = false;
            this.validateFiles.RunWorkerAsync();
        }

        /// <summary>
        /// Validate files and remove files not present on repo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValidateFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            this.progressBarFileState(ProgressBarState.Normal);
            this.progressBarFileStyle(ProgressBarStyle.Marquee);

            this.progressStatusText("Validating local storage...");

            long filesCount = Directory.GetFiles(AddonsFolder, "*", SearchOption.AllDirectories).LongLength;
            int filesDone = 0;

            foreach (string item in Directory.GetFiles(AddonsFolder, "*", SearchOption.AllDirectories))
            {
                string auxItem = item.Remove(0, AddonsFolder.Length);
                string auxPath = Path.GetDirectoryName(item);
                bool deleteFile = true;

                this.currentFileText("Validating file: " + auxItem);
                this.progressBarAllValue(Convert.ToInt32(((double)filesDone / filesCount) * 100));

                using (StreamReader sr = File.OpenText(repoReader.GetRepoFile()))
                {
                    string s = string.Empty;

                    while ((s = sr.ReadLine()) != null)
                    {
                        if (auxItem == s.Split('*')[2])
                            deleteFile = false;
                    }
                }

                if (deleteFile)
                {
                    File.Delete(item);
                    do
                    {
                        if (Directory.EnumerateDirectories(auxPath).Any() || Directory.EnumerateFiles(auxPath).Any())
                            break;
                        else
                            Directory.Delete(auxPath);

                        auxPath = Directory.GetParent(auxPath).FullName;
                    } while (auxPath != AddonsFolder.Remove(AddonsFolder.Length - 1));
                }

                filesDone++;
            }
        }

        private async void ValidateFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.currentFileText("");
            this.progressBarFileValue(0);
            this.progressBarAllValue(0);

            Properties.Settings.Default.LastRepoFileHash = repoReader.CalculateFileHash(repoReader.GetRepoFile());
            Properties.Settings.Default.Save();

            if (this.isInstall)
                this.installFiles.RunWorkerAsync();
            else
            {
                this.progressBarFileStyle(ProgressBarStyle.Continuous);
                this.progressStatusText("Waiting for orders");
                await this.taskDelay(1500);
                this.mainForm.HideDownloadPanel();
                mainForm.ReadRepo(true, true);
            }
        }

        /// <summary>
        /// Installation background worker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            bool allFine = false;

            string sourcePath = string.Empty;
            string destinationPath = string.Empty;

            try
            {
                // userconfig folder
                this.progressBarFileState(ProgressBarState.Normal);
                this.progressBarFileStyle(ProgressBarStyle.Marquee);
                this.progressStatusText("Moving userconfig folder...");

                sourcePath = AddonsFolder + @"@task_force_radio\userconfig";
                destinationPath = Properties.Settings.Default.Arma3Folder + @"userconfig";

                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));


                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);

                // TFR Plugin
                if (isTFR && !GlobalVar.isServer)
                {
                    this.progressStatusText("Installing TeamSpeak 3 plugins...");
                    this.currentFileText("Continue on TeamSpeak 3 plugin installer");

                    sourcePath = AddonsFolder + @"@task_force_radio\plugins";

                    foreach (string file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                        Process.Start(file).WaitForExit();
                }

                allFine = true;
            }
            catch (Exception ex)
            {
                this.progressBarFileState(ProgressBarState.Error);
                e.Cancel = true;
                allFine = false;
                this.progressStatusText("Something went wrong. Please try again.");
                new Windows.MessageBox().Show(ex.Message, "Installation failed", MessageBoxButtons.OK, MessageIcon.Error);
            }
            finally
            {
                if (allFine)
                    this.progressStatusText("Installation completed successfully. Cleaning up...");

                if (Directory.Exists(AddonsFolder + @"@task_force_radio\plugins"))
                    this.ts3Plugin.Enabled = true;
                else
                    this.ts3Plugin.Enabled = false;
            }
        }

        private async void InstallFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            switch (activeForm)
            {
                case "mainForm":
                    if (!e.Cancelled && this.isLaunch && this.mainForm.StartGameAfterDownload())
                    {
                        this.progressBarFileStyle(ProgressBarStyle.Marquee);
                        this.progressBarFileValue(50);
                        this.progressStatusText("Launching game...");
                        this.delayLaunch.Start();
                    }
                    else if (!e.Cancelled && this.isLaunch && !this.mainForm.StartGameAfterDownload())
                    {
                        this.progressBarFileStyle(ProgressBarStyle.Continuous);
                        this.progressStatusText("Game ready to launch...");
                        await this.taskDelay(1500);
                        this.mainForm.HideDownloadPanel();
                    }
                    else if (!e.Cancelled)
                    {
                        this.progressBarFileStyle(ProgressBarStyle.Continuous);
                        this.progressStatusText("Waiting for orders");
                        await this.taskDelay(1500);
                        this.mainForm.HideDownloadPanel();
                    }

                    this.isLaunch = false;

                    // unlock directory fields
                    this.gamePathBox.Enabled = true;
                    this.gamePathErase.Enabled = true;
                    this.gamePathFind.Enabled = true;

                    this.ts3PathBox.Enabled = true;
                    this.ts3PathErase.Enabled = true;
                    this.ts3PathFind.Enabled = true;

                    this.addonsPathBox.Enabled = true;
                    this.addonsPathErase.Enabled = true;
                    this.addonsPathFind.Enabled = true;

                    break;
            }

            this.progressBarAllValue(0);
            this.progressBarFileValue(0);
            this.progressBarFileState(ProgressBarState.Normal);
            this.progressBarFileStyle(ProgressBarStyle.Continuous);

            // unlock controls
            foreach (PackBlock item in flowpanelAddonPacks.Controls)
            {
                item.enablePlayButton();
            }
            this.repoValidateBtn.Enabled = true;

            this.installationRunning = false;
            GlobalVar.isInstalling = false;

            if (Directory.Exists(TempFolder))
                Directory.Delete(TempFolder, true);

            mainForm.ReadRepo(true);
        }

        /// <summary>
        /// Check if is installing addons
        /// </summary>
        /// <returns></returns>
        public bool isInstalling()
        {
            return this.installationRunning;
        }

        /// <summary>
        /// Delay a task, like Sleep
        /// </summary>
        /// <param name="delayMs"></param>
        /// <returns></returns>
        private async Task taskDelay(int delayMs)
        {
            await Task.Delay(delayMs);
        }

        /// <summary>
        /// Gives some delay to the launch process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelayLaunch_Tick(object sender, EventArgs e)
        {
            this.delayLaunch.Stop();
            this.mainForm.LaunchGame();
        }

        public void InstallTeamSpeakPlugin()
        {
            this.AddonsFolder = Properties.Settings.Default.AddonsFolder;

            if (Directory.Exists(AddonsFolder + @"@task_force_radio\plugins"))
            {
                string sourcePath = AddonsFolder + @"@task_force_radio\plugins";

                foreach (string file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                    if (file.EndsWith(".ts3_plugin")) { Process.Start(file).WaitForExit(); }

                mainForm.ShowSnackBar("Task Force Radio plugins installed successfully", 2500, true, true, Primary.LightGreen800);
            }
            else
            {
                new Windows.MessageBox().Show("No such directory \"" + AddonsFolder + @"@task_force_radio\plugins" + "\".", "No such file or directory", MessageBoxButtons.OK, MessageIcon.Error);
            }
        }

        public void InstallUserConfig()
        {
            this.AddonsFolder = Properties.Settings.Default.AddonsFolder;

            if (Directory.Exists(AddonsFolder + @"@task_force_radio\userconfig"))
            {
                string sourcePath = AddonsFolder + @"@task_force_radio\userconfig";
                string destinationPath = Properties.Settings.Default.Arma3Folder + @"userconfig";

                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));


                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);

                mainForm.ShowSnackBar("Userconfig files moved successfully", 2500, true, true, Primary.LightGreen800);
            }
            else
            {
                new Windows.MessageBox().Show("No such directory \"" + AddonsFolder + @"@task_force_radio\userconfig" + "\".", "No such file or directory", MessageBoxButtons.OK, MessageIcon.Error);
            }
        }
    }
}
