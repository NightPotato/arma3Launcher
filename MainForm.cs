﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using arma3Launcher.Workers;
using arma3Launcher.Effects;
using System.Threading.Tasks;

namespace arma3Launcher
{
    public partial class MainForm : Form
    {
        //Feed FeedMethod;
        private zCheckUpdate QuickUpdateMethod;
        private zCheckUpdate UpdateMethod;
        private LaunchCore PrepareLaunch;
        private Packs fetchAddonPacks;
        private EmailReporter eReport;
        private AddonsLooker aLooker;
        private Downloader downloader;
        private Installer installer;
        private RemoteReader remoteReader;
        private WindowIO windowIO;
        private PanelIO addonsPanelIO;
        private PanelIO communityPanelIO;
        private PanelIO launchoptionsPanelIO;
        private PanelIO helpPanelIO;
        private PanelIO aboutPanelIO;
        private PanelIO topPanelsIO;
        private PanelIO botPanelIO;

        private Windows.Splash loadingSplash;

        private Version aLocal = null;
        private Version aRemote = null;

        private Button activeButton;
        private int aux_Blinker = 0;

        private string armaDir_previousDir = "";
        private string tsDir_previousDir = "";
        private string modsDir_previousDir = "";

        private string activePack = "";
        private string GameFolder = "";
        private string TSFolder = "";
        private string AddonsFolder = "";

        private string oldVersionStatusText = "";

        private bool isBlastcoreAllowed = false;
        private bool isDragonFyreAllowed = false;
        private bool isOptionalAllowed = false;

        private string TempFolder = Path.GetTempPath() + @"arma3Launcher\";
        private List<string> modsName = new List<string>();
        private List<string> modsUrl = new List<string>();
        private string cfgFile = "";
        private string cfgUrl = "";

        private string blastcoreUrl = "";
        private string blastcoreName = "";

        private string dragonfyreUrl = "";
        private string dragonfyreName = "";

        private string dragonfyrerhsUrl = "";
        private string dragonfyrerhsName = "";

        private string Arguments = "";

        private bool isActive = true;
        private bool isUpdate = false;

        private int menuSelected = 0;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void WindowTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void WindowVersionStatus_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == SingleInstance.WM_SHOWFIRSTINSTANCE)
            {
                WinApi.ShowToFront(this.Handle);
                this.Show();
                this.TopMost = true;
                Thread.Sleep(1);
                this.TopMost = false;
            }
            base.WndProc(ref m);
        }

        public MainForm()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);

            InitializeComponent();

            txt_appTitle.Text = AssemblyTitle;
            txt_appVersion.Text = AssemblyVersion;

            QuickUpdateMethod = new zCheckUpdate(WindowVersionStatus, busy);
            UpdateMethod = new zCheckUpdate(btn_update, btn_checkUpdates, txt_curversion, txt_latestversion, busy);

            installer = new Installer(this, prb_progressBar_File, prb_progressBar_All, txt_progressStatus, txt_percentageStatus, txt_curFile, btn_Launch, btn_cancelDownload, txtb_armaDirectory, txtb_tsDirectory, txtb_modsDirectory, btn_ereaseArmaDirectory, btn_ereaseTSDirectory, btn_ereaseModsDirectory, btn_browseA3, btn_browseTS3, btn_browseModsDirectory, btn_reinstallTFRPlugins, btn_downloadDragonFyre, btn_downloadBlastcore);
            downloader = new Downloader(this, installer, prb_progressBar_File, prb_progressBar_All, txt_curFile, txt_progressStatus, txt_percentageStatus, btn_Launch, btn_cancelDownload);
            remoteReader = new RemoteReader();
            fetchAddonPacks = new Packs(this, FeedContentPanel);
            eReport = new EmailReporter();
            aLooker = new AddonsLooker(lstb_detectedAddons, lstb_activeAddons, chb_dragonfyre, chb_blastcore);
            loadingSplash = new Windows.Splash();
            windowIO = new WindowIO(this);

            addonsPanelIO = new PanelIO(panel_news, Panels, 304, 306, 33);
            communityPanelIO = new PanelIO(panel_community, Panels, 304, 306, 33);
            launchoptionsPanelIO = new PanelIO(panel_launchOptions, Panels, 304, 306, 33);
            helpPanelIO = new PanelIO(panel_help, Panels, 304, 306, 33);
            aboutPanelIO = new PanelIO(panel_about, Panels, 304, 306, 33);
            topPanelsIO = new PanelIO(panelDirectories, panelMenu, 4);
            botPanelIO = new PanelIO(panel_bottomHide_Inner, panel_bottomhide, 746, 750, 53);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            loadingSplash.Show();

            this.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                          (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2);

            //FeedMethod = new Feed(FeedContentPanel, Properties.GlobalValues.FP_FeedUrl);
            //FeedMethod.GetRSSNews();
            //delayFecthNews.Start();

            if (Properties.Settings.Default.UpdateSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                if (AssemblyVersion == "0.6")
                    Properties.Settings.Default.firstLaunch = true;
                Properties.Settings.Default.Save();
            }

            if (GlobalVar.isServer) { WindowTitle.Text = AssemblyTitle + " | v" + AssemblyVersion + " | Server Edition"; }
            else { WindowTitle.Text = AssemblyTitle + " | v" + AssemblyVersion; }

            // Change stuff if isServer
            if (GlobalVar.isServer)
            {
                panel_recommendedAddons.Visible = false;
                panel_TeamSpeakDir.Visible = false;
                pref_joinServerAuto.Visible = false;
                btn_reinstallTFRPlugins.Visible = false;
                pref_serverAutopilot.Visible = true;
                chb_battleye.Enabled = false;

                pref_startGameAfterDownloadsAreCompleted.Text = "Start server when ready";

                if (!Properties.Settings.Default.firstLaunch)
                    if (new Windows.DelayServerStart().ShowDialog() == DialogResult.OK)
                        switchAutopilot(true);
                    else
                        switchAutopilot(false);
            }

            if (!GlobalVar.autoPilot && !QuickUpdateMethod.QuickCheck())
            {
                menuSelected = 4;
                HideUnhide(menuSelected);

                panelLaunch.Enabled = false;
                sysbtn_moreOptions.Visible = false;

                aboutPanelIO = new PanelIO(panel_about, Panels, 435, 437, 33);

                activeButton = btn_update;
                backgroundBlinker.RunWorkerAsync();

                isUpdate = true;
            }
            else if (Properties.Settings.Default.firstLaunch)
            {
                if (GlobalVar.isServer) { pref_startGameAfterDownloadsAreCompleted.Checked = true; }

                menuSelected = 3;
                HideUnhide(menuSelected);
            }
            else
            {
                menuSelected = 0;
                HideUnhide(menuSelected);
            }

            FetchSettings();

            if (!isUpdate)
            {
                updateCurrentPack(true);
                getMalloc();


                if (Directory.Exists(AddonsFolder + @"@task_force_radio\plugins"))
                    btn_reinstallTFRPlugins.Enabled = true;
                else
                    btn_reinstallTFRPlugins.Enabled = false;
            }

            UpdateMethod.CheckUpdates();

            loadingSplash.Close();
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            windowIO.windowIn();

            FeedContentPanel.Focus();

            if (!isUpdate)
                topPanelsIO.showPanel();

            if (Properties.Settings.Default.downloadQueue != "" && panelMenu.Visible == true)
            {
                if (GlobalVar.autoPilot || MessageBox.Show("You haven't finished all the downloads the last time you closed the launcher.\n\"Yes\", to continue downloads.\n\"No\", will DELETE your progress.", "spN Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string[] aux_downloadQueue = Properties.Settings.Default.downloadQueue.Split(',');
                    foreach (string s in aux_downloadQueue)
                    {
                        if (s != "")
                            modsUrl.Add(s);

                        if (s.Contains("DragonFyre"))
                        {
                            btn_downloadDragonFyre.Enabled = false;
                        }

                        if (s.Contains("Blastcore"))
                        {
                            btn_downloadBlastcore.Enabled = false;
                        }
                    }

                    downloader.beginDownload(modsUrl, GlobalVar.autoPilot, activePack, cfgUrl.Split('!')[1]);
                }
                else
                {
                    if (Directory.Exists(TempFolder))
                        Directory.Delete(TempFolder, true);

                    Properties.Settings.Default.downloadQueue = "";
                    Properties.Settings.Default.Save();
                }
            }
            else
            {
                if (GlobalVar.autoPilot)
                {
                    await taskDelay(2500);
                    launchProcess();
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!installer.isInstalling())
            {
                SaveSettings();
                downloader.SaveDownloadQueue();
                GC.Collect();
            }
            else
            {
                if (MessageBox.Show("The launcher is installing the addons. Do you want to cancel the process and leave?", "Installing addons", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    e.Cancel = true;
                else
                { SaveSettings(); GC.Collect(); }
            }
        }

        public void GetAddons()
        {
            aLooker.getAddons(isDragonFyreAllowed, isBlastcoreAllowed, modsName);
        }

        void FetchSettings()
        {
            // directories
            if (Properties.Settings.Default.Arma3Folder != "")
            { txtb_armaDirectory.ForeColor = Color.FromArgb(64, 64, 64); GameFolder = txtb_armaDirectory.Text = Properties.Settings.Default.Arma3Folder; }
            else
            { txtb_armaDirectory.ForeColor = Color.DarkGray; txtb_armaDirectory.Text = "Set directory ->"; }

            if (Properties.Settings.Default.TS3Folder != "")
            { txtb_tsDirectory.ForeColor = Color.FromArgb(64, 64, 64); TSFolder = txtb_tsDirectory.Text = Properties.Settings.Default.TS3Folder; }
            else
            { txtb_tsDirectory.ForeColor = Color.DarkGray; txtb_tsDirectory.Text = "Set directory ->"; }

            if (Properties.Settings.Default.AddonsFolder != "")
            { txtb_modsDirectory.ForeColor = Color.FromArgb(64, 64, 64); AddonsFolder = txtb_modsDirectory.Text = Properties.Settings.Default.AddonsFolder; }
            else
            { txtb_modsDirectory.ForeColor = Color.DarkGray; txtb_modsDirectory.Text = "Set directory ->"; }

            // launch options
            chb_noLogs.Checked = Properties.Settings.Default.noLogs;
            chb_noPause.Checked = Properties.Settings.Default.noPause;
            chb_noSplash.Checked = Properties.Settings.Default.noSplash;
            chb_noCB.Checked = Properties.Settings.Default.noCB;
            chb_enableHT.Checked = Properties.Settings.Default.enableHT;
            chb_skipIntro.Checked = Properties.Settings.Default.skipIntro;
            chb_window.Checked = Properties.Settings.Default.window;
            chb_showScriptErrors.Checked = Properties.Settings.Default.showScriptErrors;
            chb_noBenchmark.Checked = Properties.Settings.Default.noBenchmark;

            chb_world.Checked = Properties.Settings.Default.world;
            txtb_world.Text = Properties.Settings.Default.world_value;

            chb_maxMem.Checked = Properties.Settings.Default.maxMem;
            txtb_maxMem.Text = Properties.Settings.Default.maxMem_value.ToString();

            chb_malloc.Checked = Properties.Settings.Default.malloc;
            txtb_malloc.Text = Properties.Settings.Default.malloc_value.ToString();

            chb_maxVRAM.Checked = Properties.Settings.Default.maxVRAM;
            txtb_maxVRAM.Text = Properties.Settings.Default.maxVRAM_value.ToString();

            chb_exThreads.Checked = Properties.Settings.Default.exThreads;
            txtb_exThreads.Text = Properties.Settings.Default.exThreads_value.ToString();

            chb_cpuCount.Checked = Properties.Settings.Default.cpuCount;
            txtb_cpuCount.Text = Properties.Settings.Default.cpuCount_value.ToString();

            chb_dragonfyre.Checked = Properties.Settings.Default.JSRS;
            chb_blastcore.Checked = Properties.Settings.Default.BlastCore;

            // battleye
            chb_battleye.Checked = Properties.Settings.Default.battleye;
            if (Properties.Settings.Default.battleye)
                GlobalVar.gameArtifact = "arma3battleye.exe";
            else
                GlobalVar.gameArtifact = "arma3.exe";

            // optional addons
            lstb_activeAddons.Items.Clear();
            string[] aux_activeMods = Properties.Settings.Default.activeMods.Split(',');
            foreach (string s in aux_activeMods)
            {
                if (s != "")
                    lstb_activeAddons.Items.Add(s);
            }

            // preferences
            pref_startGameAfterDownloadsAreCompleted.Checked = Properties.Settings.Default.startGameAfterDownload;
            pref_runLauncherOnStartup.Checked = Properties.Settings.Default.runLauncherOnStartup;
            pref_allowNotifications.Checked = Properties.Settings.Default.allowNotifications;
            pref_allowNotifications.Checked = Properties.Settings.Default.autoDownload;
            pref_joinServerAuto.Checked = Properties.Settings.Default.joinServerAutomatically;
        }

        void SaveSettings()
        {
            // launch options
            Properties.Settings.Default.noLogs = chb_noLogs.Checked;
            Properties.Settings.Default.noPause = chb_noPause.Checked;
            Properties.Settings.Default.noSplash = chb_noSplash.Checked;
            Properties.Settings.Default.noCB = chb_noCB.Checked;
            Properties.Settings.Default.enableHT = chb_enableHT.Checked;
            Properties.Settings.Default.skipIntro = chb_skipIntro.Checked;
            Properties.Settings.Default.window = chb_window.Checked;
            Properties.Settings.Default.battleye = chb_battleye.Checked;
            Properties.Settings.Default.showScriptErrors = chb_showScriptErrors.Checked;
            Properties.Settings.Default.noBenchmark = chb_noBenchmark.Checked;

            Properties.Settings.Default.world = chb_world.Checked;
            Properties.Settings.Default.world_value = txtb_world.Text;

            Properties.Settings.Default.maxMem = chb_maxMem.Checked;
            Properties.Settings.Default.maxMem_value = Convert.ToInt32(txtb_maxMem.Text);

            Properties.Settings.Default.malloc = chb_malloc.Checked;
            Properties.Settings.Default.malloc_value = txtb_malloc.Text;

            Properties.Settings.Default.maxVRAM = chb_maxVRAM.Checked;
            Properties.Settings.Default.maxVRAM_value = Convert.ToInt32(txtb_maxVRAM.Text);

            Properties.Settings.Default.exThreads = chb_exThreads.Checked;
            Properties.Settings.Default.exThreads_value = Convert.ToInt32(txtb_exThreads.Text);

            Properties.Settings.Default.cpuCount = chb_cpuCount.Checked;
            Properties.Settings.Default.cpuCount_value = Convert.ToInt32(txtb_cpuCount.Text);

            Properties.Settings.Default.JSRS = chb_dragonfyre.Checked;
            Properties.Settings.Default.BlastCore = chb_blastcore.Checked;

            // optional addons
            string aux_activeMods = "";
            foreach (var item in lstb_activeAddons.Items)
            {
                if (aux_activeMods == "")
                    aux_activeMods = item + ",";
                else
                    aux_activeMods = aux_activeMods + item + ",";
            }
            Properties.Settings.Default.activeMods = aux_activeMods;

            // preferences
            Properties.Settings.Default.startGameAfterDownload = pref_startGameAfterDownloadsAreCompleted.Checked;
            Properties.Settings.Default.runLauncherOnStartup = pref_runLauncherOnStartup.Checked;
            Properties.Settings.Default.allowNotifications = pref_allowNotifications.Checked;
            Properties.Settings.Default.autoDownload = pref_allowNotifications.Checked;
            Properties.Settings.Default.joinServerAutomatically = pref_joinServerAuto.Checked;

            Properties.Settings.Default.Save();
        }

        public void FetchRemoteSettings(bool refreshPacks)
        {
            bool isInstalled = false;
            bool forceRefreshPacks = false;
            modsName.Clear();
            modsUrl.Clear();

            AddonsFolder = Properties.Settings.Default.AddonsFolder;

            try
            {
                XmlDocument RemoteXmlInfo = new XmlDocument();
                RemoteXmlInfo.Load(Properties.GlobalValues.S_VersionXML);

                string xmlNodes = "";
                XmlNodeList xnl;

                //Common Files
                xmlNodes = "//arma3Launcher//ModSetInfo//Recommended//mod";
                xnl = RemoteXmlInfo.SelectNodes(xmlNodes);

                foreach (XmlNode xn in xnl)
                {
                    if (xn.Attributes["type"].Value == "blastcore")
                    {
                        blastcoreName = xn.Attributes["name"].Value;
                        blastcoreUrl = xn.Attributes["url"].Value;
                        btn_downloadBlastcore.Text = "Download (" + xn.Attributes["version"].Value + ")";
                    }

                    if (xn.Attributes["type"].Value == "dragonfyre")
                    {
                        dragonfyreName = xn.Attributes["name"].Value;
                        dragonfyreUrl = xn.Attributes["url"].Value;
                        btn_downloadDragonFyre.Text = "Download (" + xn.Attributes["version"].Value + ")";
                    }

                    if (xn.Attributes["type"].Value == "dragonfyrerhs")
                    {
                        dragonfyrerhsName = xn.Attributes["name"].Value;
                        dragonfyrerhsUrl = xn.Attributes["url"].Value;
                    }
                }

                //Validate if activePack exists or select first on the list
                xmlNodes = "//arma3Launcher//ModSets//pack";
                xnl = RemoteXmlInfo.SelectNodes(xmlNodes);
                string firstPack = "";
                activePack = "";

                foreach (XmlNode xn in xnl)
                {
                    if (String.IsNullOrEmpty(firstPack) && Convert.ToBoolean(xn.Attributes["enable"].Value))
                    { firstPack = xn.Attributes["id"].Value; }

                    if (Properties.Settings.Default.lastAddonPack == xn.Attributes["id"].Value && Convert.ToBoolean(xn.Attributes["enable"].Value))
                    { activePack = Properties.Settings.Default.lastAddonPack; break; }
                }

                if (String.IsNullOrEmpty(activePack))
                { Properties.Settings.Default.lastAddonPack = activePack = firstPack; forceRefreshPacks = true; }

                isBlastcoreAllowed = Convert.ToBoolean(RemoteXmlInfo.SelectSingleNode("//arma3Launcher//ModSetInfo//" + activePack).Attributes["blastcore"].Value);
                isDragonFyreAllowed = Convert.ToBoolean(RemoteXmlInfo.SelectSingleNode("//arma3Launcher//ModSetInfo//" + activePack).Attributes["dragonfyre"].Value);
                isOptionalAllowed = Convert.ToBoolean(RemoteXmlInfo.SelectSingleNode("//arma3Launcher//ModSetInfo//" + activePack).Attributes["optional"].Value);

                cfgFile = activePack;
                cfgUrl = remoteReader.GetPackConfigFile(activePack);

                if (isBlastcoreAllowed)
                { chb_blastcore.Enabled = true; }
                else
                { chb_blastcore.Enabled = false; }

                if (isDragonFyreAllowed)
                { chb_dragonfyre.Enabled = true; }
                else
                { chb_dragonfyre.Enabled = false; }

                if (isOptionalAllowed)
                { panel_Optional.Enabled = true; }
                else
                { panel_Optional.Enabled = false; }

                xmlNodes = "//arma3Launcher//ModSetInfo//" + activePack + "//mod";
                xnl = RemoteXmlInfo.SelectNodes(xmlNodes);

                foreach (XmlNode xn in xnl)
                {
                    if (xn.Attributes["type"].Value == "mod")
                    {
                        modsName.Add(xn.Attributes["name"].Value);

                        if (AddonsFolder != "")
                        {
                            foreach (string d in Directory.GetDirectories(AddonsFolder))
                            {
                                string[] aux_d = d.Split('\\');

                                if (aux_d[aux_d.Length - 1].Equals(xn.Attributes["name"].Value))
                                {
                                    try
                                    {
                                        if (d.Contains("dummy")) { isInstalled = true; break; }

                                        foreach (var line in File.ReadAllLines(d + @"\spNversionController"))
                                        {
                                            if (line.Contains("version"))
                                            {
                                                string aux_line = line.Replace(" ", "");
                                                string[] splitted_line = aux_line.Split('=');

                                                aLocal = new Version(splitted_line[1]);
                                                aRemote = new Version(xn.Attributes["version"].Value);
                                                break;
                                            }
                                        }

                                        if (aRemote != aLocal)
                                        {
                                            if (!d.Contains("RHS"))
                                                Directory.Delete(d, true);

                                            isInstalled = false;
                                            break;
                                        }
                                        else { isInstalled = true; break; }
                                    }
                                    catch (Exception ex)
                                    {
                                        //MessageBox.Show(ex.Message);
                                    }
                                }
                                else { isInstalled = false; continue; }
                            }
                        }

                        if (!isInstalled && Properties.Settings.Default.downloadQueue == "")
                            modsUrl.Add(xn.Attributes["url"].Value);
                    }
                }

                if (modsUrl.Count > 0)
                    modsUrl.Insert(0, cfgUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to fetch remote settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_progressStatus.Text = "Unable to fetch remote settings.";
            }
            finally
            {
                if (refreshPacks || forceRefreshPacks)
                    fetchAddonPacks.Get();
            }
        }

        void getMalloc()
        {
            txtb_malloc.Items.Clear();

            try
            {
                string[] fileEntries = Directory.GetFiles(Properties.Settings.Default.Arma3Folder + "Dll\\", "*.dll");
                foreach (string fileName in fileEntries)
                {
                    txtb_malloc.Items.Add(Path.GetFileName(fileName).Remove(Path.GetFileName(fileName).Length - 4));
                }
            }
            catch
            { }
        }

        #region Assembly Info
        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                string aux = "";
                if (Assembly.GetExecutingAssembly().GetName().Version.Build != 0)
                    aux = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString() + "." + Assembly.GetExecutingAssembly().GetName().Version.Build.ToString() /*+ "." + Assembly.GetExecutingAssembly().GetName().Version.Revision.ToString()*/;
                else
                    aux = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
                return aux;
            }
        }
        #endregion

        #region System Buttons
        private void sysbtn_close_Click(object sender, EventArgs e)
        {
            windowIO.windowOut(true);
        }

        private void sysbtn_minimize_Click(object sender, EventArgs e)
        {
            minimizeWindow();
        }

        private void sysbtn_moreOptions_Click(object sender, EventArgs e)
        {
            menu_moreOptions.Show(sysbtn_moreOptions, 0, 18);
        }

        private void sysbtn_close_MouseEnter(object sender, EventArgs e)
        {
            sysbtn_close.Image = Properties.Resources.bgclose2;
        }

        private void sysbtn_close_MouseLeave(object sender, EventArgs e)
        {
            if (isActive)
                sysbtn_close.Image = Properties.Resources.bgclose1;
            else
                sysbtn_close.Image = Properties.Resources.bgclose3;
        }
        private void sysbtn_close_MouseDown(object sender, MouseEventArgs e)
        {
            sysbtn_close.Image = Properties.Resources.bgclose4;
        }

        private void sysbtn_minimize_MouseEnter(object sender, EventArgs e)
        {
            sysbtn_minimize.Image = Properties.Resources.bgminimize2;
        }

        private void sysbtn_minimize_MouseLeave(object sender, EventArgs e)
        {
            if (isActive)
                sysbtn_minimize.Image = Properties.Resources.bgminimize1;
            else
                sysbtn_minimize.Image = Properties.Resources.bgminimize3;
        }

        private void sysbtn_minimize_MouseDown(object sender, MouseEventArgs e)
        {
            sysbtn_minimize.Image = Properties.Resources.bgminimize4;
        }

        private void sysbtn_moreOptions_MouseDown(object sender, MouseEventArgs e)
        {
            sysbtn_moreOptions.Image = Properties.Resources.bgmore4_fw;
        }

        private void sysbtn_moreOptions_MouseEnter(object sender, EventArgs e)
        {
            sysbtn_moreOptions.Image = Properties.Resources.bgmore2_fw;
        }

        private void sysbtn_moreOptions_MouseLeave(object sender, EventArgs e)
        {
            if (isActive)
                sysbtn_moreOptions.Image = Properties.Resources.bgmore1_fw;
            else
                sysbtn_moreOptions.Image = Properties.Resources.bgmore3_fw;
        }
        #endregion

        private void btn_browseA3_Click(object sender, EventArgs e)
        {
            this.browseGameFolder();
        }

        private void btn_browseTS3_Click(object sender, EventArgs e)
        {
            this.browseTSFolder();
        }

        private void browseGameFolder()
        {
            dlg_folderBrowser.Description = "Select Arma 3 root folder.";
            if (Directory.Exists(txtb_armaDirectory.Text))
                dlg_folderBrowser.SelectedPath = txtb_armaDirectory.Text;

            if (dlg_folderBrowser.ShowDialog() == DialogResult.OK)
            {
                string auxA3Folder = dlg_folderBrowser.SelectedPath;
                bool auxIsFolder = false;

                try
                {
                    foreach (string f in Directory.GetFiles(auxA3Folder))
                    {
                        if ((f.Contains(GlobalVar.gameArtifact) && !GlobalVar.isServer) || (f.Contains("arma3server.exe") && GlobalVar.isServer)) { auxIsFolder = true; break; }
                        else { continue; }
                    }
                }
                catch
                { }
                finally
                {
                    if (auxIsFolder)
                    {
                        txtb_armaDirectory.ForeColor = Color.FromArgb(64, 64, 64);
                        GameFolder = Properties.Settings.Default.Arma3Folder = auxA3Folder + @"\";
                        Properties.Settings.Default.Save();
                        txtb_armaDirectory.Text = auxA3Folder;
                    }
                    else
                    {
                        MessageBox.Show("Game executable not there. Please check your Arma 3 directory and try again.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            dlg_folderBrowser.SelectedPath = "";
        }

        private void browseTSFolder()
        {
            dlg_folderBrowser.Description = "Select TeamSpeak 3 root folder.";
            if (Directory.Exists(txtb_tsDirectory.Text))
                dlg_folderBrowser.SelectedPath = txtb_tsDirectory.Text;

            if (dlg_folderBrowser.ShowDialog() == DialogResult.OK)
            {
                string auxTS3Folder = dlg_folderBrowser.SelectedPath;
                bool auxIsFolder = false;

                try
                {
                    foreach (string f in Directory.GetFiles(auxTS3Folder))
                    {
                        if (f.Contains("ts3client_win64.exe") || f.Contains("ts3client_win32.exe")) { auxIsFolder = true; break; }
                        else { continue; }
                    }
                }
                catch
                { }
                finally
                {
                    if (auxIsFolder)
                    {
                        txtb_tsDirectory.ForeColor = Color.FromArgb(64, 64, 64);
                        TSFolder = Properties.Settings.Default.TS3Folder = auxTS3Folder + @"\";
                        Properties.Settings.Default.Save();
                        txtb_tsDirectory.Text = auxTS3Folder;
                    }
                    else
                    {
                        MessageBox.Show("TeamSpeak executable not there. Please check your TeamSpeak directory and try again.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            dlg_folderBrowser.SelectedPath = "";
        }

        private void browseAddonsFolder()
        {
            dlg_folderBrowser.ShowNewFolderButton = true;
            dlg_folderBrowser.Description = "Select the Addons folder or create a new one.\n⚠ It can't be the same as the Game folder.";
            if (Directory.Exists(txtb_modsDirectory.Text))
                dlg_folderBrowser.SelectedPath = txtb_modsDirectory.Text;

            if (dlg_folderBrowser.ShowDialog() == DialogResult.OK)
            {
                if (dlg_folderBrowser.SelectedPath != GameFolder || GlobalVar.isServer)
                {
                    AddonsFolder = Properties.Settings.Default.AddonsFolder = dlg_folderBrowser.SelectedPath + @"\";
                    Properties.Settings.Default.Save();
                    txtb_modsDirectory.Text = dlg_folderBrowser.SelectedPath;
                    GetAddons();
                }
                else
                {
                    MessageBox.Show("The Addons folder can't be the same as the Game folder.\nWe recommend you to have a specific folder for the addons on this launcher to avoid conflicts.", "Wrong directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.browseAddonsFolder();
                }
            }

            dlg_folderBrowser.SelectedPath = "";
            dlg_folderBrowser.ShowNewFolderButton = false;
        }

        /*-----------------------------------
            START MENU FUNCTIONS
         * Function Hide/Unhide
         * Click
         * Mouse Enter
         * Mouse Leave
        -----------------------------------*/

        #region Menu Region
        /*-----------------------------------
            Hide/Unhide
        -----------------------------------*/
        private bool _isTop = false;
        private async void HideUnhide(int selectedOption)
        {
            if (!GlobalVar.isAnimating)
            {
                if (panel_news.Height > 0) { Panels.BackColor = Color.DimGray; addonsPanelIO.hidePanel(); menu_news.ForeColor = Color.Gray; }
                if (panel_community.Height > 0) { Panels.BackColor = Color.DimGray; communityPanelIO.hidePanel(); menu_community.ForeColor = Color.Gray; }
                if (panel_launchOptions.Height > 0) { Panels.BackColor = Color.DimGray; launchoptionsPanelIO.hidePanel(); menu_launchOptions.ForeColor = Color.Gray; }
                if (panel_help.Height > 0) { Panels.BackColor = Color.DimGray; helpPanelIO.hidePanel(); menu_help.ForeColor = Color.Gray; }
                if (panel_about.Height > 0) { Panels.BackColor = Color.DimGray; aboutPanelIO.hidePanel(); menu_about.ForeColor = Color.Gray; }

                await taskDelay(600);

                if (selectedOption == 0) { Panels.BackColor = Color.OliveDrab; menu_news.ForeColor = Color.OliveDrab; addonsPanelIO.showPanel(); }
                if (selectedOption == 1) { Panels.BackColor = Color.OliveDrab; menu_community.ForeColor = Color.OliveDrab; communityPanelIO.showPanel(); }
                if (selectedOption == 2) { Panels.BackColor = Color.OliveDrab; menu_launchOptions.ForeColor = Color.OliveDrab; launchoptionsPanelIO.showPanel(); }
                if (selectedOption == 3) { Panels.BackColor = Color.OliveDrab; menu_help.ForeColor = Color.OliveDrab; helpPanelIO.showPanel(); }
                if (selectedOption == 4) { Panels.BackColor = Color.OliveDrab; menu_about.ForeColor = Color.OliveDrab; aboutPanelIO.showPanel(); }
            }
        }

        /*-----------------------------------
            Menu News
        -----------------------------------*/
        private void menu_news_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isAnimating)
            {
                menuSelected = 0;
                HideUnhide(menuSelected);
            }
        }

        private void menu_news_MouseEnter(object sender, EventArgs e)
        {
            menu_news.ForeColor = Color.DarkGray;
        }

        private void menu_news_MouseLeave(object sender, EventArgs e)
        {
            if (menuSelected != 0)
                menu_news.ForeColor = Color.Gray;
            else
                menu_news.ForeColor = Color.OliveDrab;
        }

        /*-----------------------------------
            Menu spN Community
        -----------------------------------*/
        private void menu_community_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isAnimating)
            {
                menuSelected = 1;
                HideUnhide(menuSelected);
            }
        }

        private void menu_community_MouseEnter(object sender, EventArgs e)
        {
            menu_community.ForeColor = Color.DarkGray;
        }

        private void menu_community_MouseLeave(object sender, EventArgs e)
        {
            if (menuSelected != 1)
                menu_community.ForeColor = Color.Gray;
            else
                menu_community.ForeColor = Color.OliveDrab;
        }

        /*-----------------------------------
            Menu Launch & Addons Options
        -----------------------------------*/
        private void menu_launchOptions_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isAnimating)
            {
                menuSelected = 2;
                HideUnhide(menuSelected);
            }
        }

        private void menu_launchOptions_MouseEnter(object sender, EventArgs e)
        {
            menu_launchOptions.ForeColor = Color.DarkGray;
        }

        private void menu_launchOptions_MouseLeave(object sender, EventArgs e)
        {
            if (menuSelected != 2)
                menu_launchOptions.ForeColor = Color.Gray;
            else
                menu_launchOptions.ForeColor = Color.OliveDrab;
        }

        /*-----------------------------------
            Menu Help
        -----------------------------------*/
        private void menu_help_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isAnimating)
            {
                menuSelected = 3;
                HideUnhide(menuSelected);
            }
        }

        private void menu_help_MouseEnter(object sender, EventArgs e)
        {
            menu_help.ForeColor = Color.DarkGray;
        }

        private void menu_help_MouseLeave(object sender, EventArgs e)
        {
            if (menuSelected != 3)
                menu_help.ForeColor = Color.Gray;
            else
                menu_help.ForeColor = Color.OliveDrab;
        }

        /*-----------------------------------
            Menu About
        -----------------------------------*/
        private void menu_about_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isAnimating)
            {
                menuSelected = 4;
                HideUnhide(menuSelected);
            }
        }

        private void menu_about_MouseEnter(object sender, EventArgs e)
        {
            menu_about.ForeColor = Color.DarkGray;
        }

        private void menu_about_MouseLeave(object sender, EventArgs e)
        {
            if (menuSelected != 4)
                menu_about.ForeColor = Color.Gray;
            else
                menu_about.ForeColor = Color.OliveDrab;
        }

        /*-----------------------------------
            END MENU FUNCTIONS
        -----------------------------------*/

        #endregion

        /*-----------------------------------
            START UPDATE FUNCTIONS (!! TO BE MOVED !!)
         * Update btn Click
         * StartUpdator()
        -----------------------------------*/

        #region Update Functions
        private void btn_update_Click(object sender, EventArgs e)
        {
            StartUpdator();
            Thread.Sleep(500);
            windowIO.windowOut(true);
        }

        void StartUpdator()
        {
            try
            {
                WebClient update_file = new WebClient();
                Uri update_url = new Uri(Properties.GlobalValues.S_UpdateUrl);

                update_file.DownloadFile(update_url, "zUpdator.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                var fass = new ProcessStartInfo();
                fass.FileName = "zUpdator.exe";
                fass.Arguments = "-curversion=" + txt_curversion.Text + 
                    " -newversion=" + txt_latestversion.Text +
                    " -filename=" + Process.GetCurrentProcess().MainModule.ModuleName;

                var process = new Process();
                process.StartInfo = fass;
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        /*-----------------------------------
            END UPDATE FUNCTIONS
        -----------------------------------*/

        #region Recommended Addons
        private void btn_DragonFyre_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.armaholic.com/page.php?id=27827");
        }

        private void btn_Blastcore_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.armaholic.com/page.php?id=23899");
        }
        #endregion

        #region Game Options Conditions
        private void chb_world_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_world.Checked)
                txtb_world.Enabled = true;
            else
                txtb_world.Enabled = false;
        }

        private void chb_maxMem_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_maxMem.Checked)
                txtb_maxMem.Enabled = true;
            else
                txtb_maxMem.Enabled = false;
        }

        private void chb_malloc_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_malloc.Checked)
                txtb_malloc.Enabled = true;
            else
                txtb_malloc.Enabled = false;
        }

        private void chb_maxVRAM_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_maxVRAM.Checked)
                txtb_maxVRAM.Enabled = true;
            else
                txtb_maxVRAM.Enabled = false;
        }

        private void chb_exThreads_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_exThreads.Checked)
                txtb_exThreads.Enabled = true;
            else
                txtb_exThreads.Enabled = false;
        }

        private void chb_cpuCount_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_cpuCount.Checked)
                txtb_cpuCount.Enabled = true;
            else
                txtb_cpuCount.Enabled = false;
        }
        #endregion

        private void btn_Launch_Click(object sender, EventArgs e)
        {
            btn_Launch.Focus();
            launchProcess();
        }

        public void reLaunchServer()
        {
            if (new Windows.DelayServerStart().ShowDialog() == DialogResult.OK)
                switchAutopilot(true);
            else
                switchAutopilot(false);

            if (GlobalVar.autoPilot)
                launchProcess();
        }

        private void launchProcess()
        {
            if ((Directory.Exists(TSFolder) && (File.Exists(TSFolder + "ts3client_win64.exe") || File.Exists(TSFolder + "ts3client_win32.exe")) || GlobalVar.isServer))
            {
                if (Directory.Exists(GameFolder) && ((File.Exists(GameFolder + GlobalVar.gameArtifact) && !GlobalVar.isServer) || (File.Exists(GameFolder + "arma3server.exe") && GlobalVar.isServer)))
                {
                    if (Directory.Exists(AddonsFolder))
                    {
                        updateCurrentPack(false);

                        btn_Launch.Enabled = false;

                        PrepareLaunch = new LaunchCore(chb_noLogs.Checked,
                            chb_noPause.Checked,
                            chb_noSplash.Checked,
                            chb_noCB.Checked,
                            chb_enableHT.Checked,
                            chb_skipIntro.Checked,
                            chb_window.Checked,
                            chb_showScriptErrors.Checked,
                            chb_noBenchmark.Checked,
                            chb_world.Checked,
                            txtb_world.Text,
                            chb_maxMem.Checked,
                            txtb_maxMem.Text,
                            chb_malloc.Checked,
                            txtb_malloc.Text,
                            chb_maxVRAM.Checked,
                            txtb_maxVRAM.Text,
                            chb_exThreads.Checked,
                            txtb_exThreads.Text,
                            chb_cpuCount.Checked,
                            txtb_cpuCount.Text,
                            chb_dragonfyre.Checked,
                            dragonfyreName,
                            dragonfyrerhsName,
                            chb_blastcore.Checked,
                            blastcoreName,
                            lstb_activeAddons,
                            modsName);

                        Arguments = PrepareLaunch.GetArguments();
                        SaveSettings();

                        if (activePack == "arma3" || PrepareLaunch.isModPackInstalled(modsName, modsUrl))
                            runGame();
                        else
                        {
                            if (!GlobalVar.isDownloading && !GlobalVar.isInstalling)
                                downloader.beginDownload(modsUrl, true, activePack, cfgUrl.Split('!')[1]);
                            else
                                MessageBox.Show("There's a download already in progress. Please wait for it to finish.", "Download already in progress", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Addons directory doesn't exist. Please check your Addons directory and try again.", "Missing directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.browseAddonsFolder();
                    }
                }
                else
                {
                    MessageBox.Show("Game directory doesn't exist or executable not there. Please check your Arma 3 directory and try again.", "Missing directory or file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.browseGameFolder();
                }
            }
            else
            {
                MessageBox.Show("TeamSpeak directory doesn't exist or executable not there. Please check your TeamSpeak directory and try again.", "Missing directory or file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.browseTSFolder();
            }
        }

        private void txtb_armaDirectory_TextChanged(object sender, EventArgs e)
        {
            if (txtb_armaDirectory.Text != "Set directory ->")
            {
                txtb_armaDirectory.ForeColor = Color.FromArgb(64, 64, 64);

                if (txtb_armaDirectory.Text.EndsWith("\\"))
                    txtb_armaDirectory.Text = txtb_armaDirectory.Text.Remove(txtb_armaDirectory.Text.Length - 1);

                if (txtb_armaDirectory.Text.EndsWith("/"))
                    txtb_armaDirectory.Text = txtb_armaDirectory.Text.Remove(txtb_armaDirectory.Text.Length - 1).Replace("/", "\\");

                if (Directory.Exists(txtb_armaDirectory.Text) && ((File.Exists(txtb_armaDirectory.Text + @"\arma3battleye.exe") && !GlobalVar.isServer) || (File.Exists(txtb_armaDirectory.Text + @"\arma3server.exe") && GlobalVar.isServer)))
                {
                    GameFolder = Properties.Settings.Default.Arma3Folder = txtb_armaDirectory.Text + @"\";
                    Properties.Settings.Default.Save();

                    getMalloc();
                    armaDir_previousDir = txtb_armaDirectory.Text;
                }
                else
                {
                    MessageBox.Show("Game executable not there. Please check your Arma 3 directory and try again.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (String.IsNullOrEmpty(armaDir_previousDir))
                        this.browseGameFolder();
                    else
                        txtb_armaDirectory.Text = armaDir_previousDir;
                }
            }
        }

        private void txtb_tsDirectory_TextChanged(object sender, EventArgs e)
        {
            if (txtb_tsDirectory.Text != "Set directory ->")
            {
                txtb_tsDirectory.ForeColor = Color.FromArgb(64, 64, 64);

                if (txtb_tsDirectory.Text.EndsWith("\\"))
                    txtb_tsDirectory.Text = txtb_tsDirectory.Text.Remove(txtb_tsDirectory.Text.Length - 1);

                if (txtb_tsDirectory.Text.EndsWith("/"))
                    txtb_tsDirectory.Text = txtb_tsDirectory.Text.Remove(txtb_tsDirectory.Text.Length - 1).Replace("/", "\\");

                if (Directory.Exists(txtb_tsDirectory.Text) && (File.Exists(txtb_tsDirectory.Text + @"\ts3client_win64.exe") || File.Exists(txtb_tsDirectory.Text + @"\ts3client_win32.exe")))
                {
                    TSFolder = Properties.Settings.Default.TS3Folder = txtb_tsDirectory.Text + @"\";
                    Properties.Settings.Default.Save();

                    tsDir_previousDir = txtb_tsDirectory.Text;
                }
                else
                {
                    MessageBox.Show("TeamSpeak executable not there. Please check your TeamSpeak directory and try again.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (String.IsNullOrEmpty(tsDir_previousDir))
                        this.browseTSFolder();
                    else
                        txtb_tsDirectory.Text = tsDir_previousDir;
                }
            }
        }

        private void txtb_modsDirectory_TextChanged(object sender, EventArgs e)
        {
            if (txtb_modsDirectory.Text != "Set directory ->")
            {
                txtb_modsDirectory.ForeColor = Color.FromArgb(64, 64, 64);

                if (txtb_modsDirectory.Text.EndsWith("\\"))
                    txtb_modsDirectory.Text = txtb_modsDirectory.Text.Remove(txtb_modsDirectory.Text.Length - 1);

                if (txtb_modsDirectory.Text.EndsWith("/"))
                    txtb_modsDirectory.Text = txtb_modsDirectory.Text.Remove(txtb_modsDirectory.Text.Length - 1).Replace("/", "\\");

                if ((txtb_modsDirectory.Text != txtb_armaDirectory.Text && !File.Exists(txtb_modsDirectory.Text + "\\arma3.exe")) || GlobalVar.isServer)
                {
                    AddonsFolder = Properties.Settings.Default.AddonsFolder = txtb_modsDirectory.Text + @"\";
                    Properties.Settings.Default.Save();

                    GetAddons();
                    modsDir_previousDir = txtb_modsDirectory.Text;
                }
                else
                {
                    MessageBox.Show("The Addons folder can't be the same as the Game folder.\nWe recommend you to have a specific folder for the addons on this launcher to avoid conflicts.", "Wrong directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (String.IsNullOrEmpty(modsDir_previousDir))
                        this.browseAddonsFolder();
                    else
                        txtb_modsDirectory.Text = modsDir_previousDir;
                }
            }
        }

        private void btn_ereaseArmaDirectory_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Arma3Folder = "";
            Properties.Settings.Default.Save();

            txtb_armaDirectory.ForeColor = Color.DarkGray; txtb_armaDirectory.Text = "Set directory ->";
        }

        private void btn_ereaseTSDirectory_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.TS3Folder = "";
            Properties.Settings.Default.Save();

            txtb_tsDirectory.ForeColor = Color.DarkGray; txtb_tsDirectory.Text = "Set directory ->";
        }

        private void btn_copyLaunchOptions_Click(object sender, EventArgs e)
        {
            PrepareLaunch = new LaunchCore(chb_noLogs.Checked,
                chb_noPause.Checked,
                chb_noSplash.Checked,
                chb_noCB.Checked,
                chb_enableHT.Checked,
                chb_skipIntro.Checked,
                chb_window.Checked,
                chb_showScriptErrors.Checked,
                chb_noBenchmark.Checked,
                chb_world.Checked,
                txtb_world.Text,
                chb_maxMem.Checked,
                txtb_maxMem.Text,
                chb_malloc.Checked,
                txtb_malloc.Text,
                chb_maxVRAM.Checked,
                txtb_maxVRAM.Text,
                chb_exThreads.Checked,
                txtb_exThreads.Text,
                chb_cpuCount.Checked,
                txtb_cpuCount.Text);

            string Arguments = PrepareLaunch.GetArguments();
            if (Arguments != "" && Arguments != null)
            {
                Clipboard.SetText(Arguments);
                MessageBox.Show("This is on your clipboard:\n" + Arguments, "Launch options copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            { MessageBox.Show("Select any option before trying to copy", "Launch options copy failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void backgroundBlinker_DoWork(object sender, DoWorkEventArgs e)
        {
            do
            {
                activeButton.FlatStyle = FlatStyle.Flat;
                activeButton.BackColor = Color.YellowGreen;
                Thread.Sleep(800);
                activeButton.FlatStyle = FlatStyle.Standard;
                activeButton.BackColor = Color.Transparent;
                Thread.Sleep(400);
            } while (aux_Blinker == 0);
        }

        private void btn_activateAddon_Click(object sender, EventArgs e)
        {
            if (lstb_activeAddons.Items.Count > 0)
                lstb_activeAddons.SetSelected(0, false);

            try
            {
                lstb_activeAddons.Items.Add(lstb_detectedAddons.SelectedItem);
                lstb_detectedAddons.Items.Remove(lstb_detectedAddons.SelectedItem);

                SaveSettings();
                lstb_detectedAddons.Focus();
                if (lstb_detectedAddons.Items.Count > 0)
                    lstb_detectedAddons.SelectedIndex = 0;
                lstb_detectedAddons.Select();
            }
            catch
            { }
        }

        private void btn_deactivateAddon_Click(object sender, EventArgs e)
        {
            if (lstb_detectedAddons.Items.Count > 0)
                lstb_detectedAddons.SetSelected(0, false);

            try
            {
                lstb_detectedAddons.Items.Add(lstb_activeAddons.SelectedItem);
                lstb_activeAddons.Items.Remove(lstb_activeAddons.SelectedItem);

                SaveSettings();
                lstb_activeAddons.Focus();
                if (lstb_activeAddons.Items.Count > 0)
                    lstb_activeAddons.SelectedIndex = 0;
                lstb_activeAddons.Select();
            }
            catch
            { }
        }

        private void btn_reloadAddons_Click(object sender, EventArgs e)
        {
            updateCurrentPack(false);
        }

        private void btn_Launch_MouseEnter(object sender, EventArgs e)
        {
            if (btn_Launch.Enabled)
                btn_Launch.Image = Properties.Resources.rocket_launch;
        }

        private void btn_Launch_MouseLeave(object sender, EventArgs e)
        {
            btn_Launch.Image = Properties.Resources.rocket;
        }

        private void btn_Launch_EnabledChanged(object sender, EventArgs e)
        {
            if (btn_Launch.Enabled)
                this.Cursor = Cursors.Default;
            else
                this.Cursor = Cursors.AppStarting;
        }

        private void btn_goTwitter_Click(object sender, EventArgs e)
        {
            Process.Start(Properties.GlobalValues.Link_Twitter);
        }

        private void btn_goTwitch_Click(object sender, EventArgs e)
        {
            Process.Start(Properties.GlobalValues.Link_Twitch);
        }

        private void btn_goYoutube_Click(object sender, EventArgs e)
        {
            Process.Start(Properties.GlobalValues.Link_Youtube);
        }

        private void btn_goGit_Click(object sender, EventArgs e)
        {
            Process.Start(Properties.GlobalValues.Link_Gihub);
        }

        private void btn_downloadDragonFyre_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isDownloading && !GlobalVar.isInstalling)
            {
                modsUrl.Clear();
                modsUrl.Add(dragonfyreUrl);
                modsUrl.Add(dragonfyrerhsUrl);
                downloader.beginDownload(modsUrl, false, activePack, cfgUrl.Split('!')[1]);
            }
            else
            {
                downloader.enqueueUrl(dragonfyreUrl);
                downloader.enqueueUrl(dragonfyrerhsUrl);
            }

            btn_downloadDragonFyre.Enabled = false;
        }

        private void btn_downloadBlastcore_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isDownloading && !GlobalVar.isInstalling)
            {
                modsUrl.Clear();
                modsUrl.Add(blastcoreUrl);
                downloader.beginDownload(modsUrl, false, activePack, cfgUrl.Split('!')[1]);
            }
            else
                downloader.enqueueUrl(blastcoreUrl);

            btn_downloadBlastcore.Enabled = false;
        }

        private void btn_downloadConfigs_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isDownloading && !GlobalVar.isInstalling)
            {
                modsUrl.Clear();
                modsUrl.Add(cfgUrl);
                downloader.beginDownload(modsUrl, false, activePack, cfgUrl.Split('!')[1]);
            }

            btn_downloadConfigs.Enabled = false;
        }

        private void btn_ereaseModsDirectory_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.AddonsFolder = "";
            Properties.Settings.Default.Save();

            txtb_modsDirectory.ForeColor = Color.DarkGray; txtb_modsDirectory.Text = "Set directory ->";
        }

        private void btn_browseModsDirectory_Click(object sender, EventArgs e)
        {
            this.browseAddonsFolder();
        }

        private void btn_openA3_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Arma3Folder != "")
                Process.Start(Properties.Settings.Default.Arma3Folder);
        }

        private void btn_openTS3_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.TS3Folder != "")
                Process.Start(Properties.Settings.Default.TS3Folder);
        }

        private void btn_openModsDirectory_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.AddonsFolder != "")
                Process.Start(Properties.Settings.Default.AddonsFolder);
        }

        private void backgroundFetchNews_DoWork(object sender, DoWorkEventArgs e)
        {
            //FeedMethod.GetRSSNews();
        }

        private void delayFecthNews_Tick(object sender, EventArgs e)
        {
            delayFecthNews.Stop();
            backgroundFetchNews.RunWorkerAsync();
        }

        private void backgroundFetchNews_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            delayFecthNews.Start();
        }

        private void btn_reloadRemoteSettings_Click(object sender, EventArgs e)
        {
            updateCurrentPack(true);
        }

        private void btn_showRemoteSettings_Click(object sender, EventArgs e)
        {
            string aux_listMods = "";

            foreach (var mod in modsName)
            {
                if (mod != null)
                {
                    aux_listMods = aux_listMods + " " + mod + ";";
                }
                else
                    break;
            }

            MessageBox.Show("Temp Path: " + TempFolder + "\nConfig File: " + cfgFile + "\nGame Server: " + remoteReader.ServerInfo(activePack)[0] + ":" + remoteReader.ServerInfo(activePack)[1] + "\n\nActive Mods:" + aux_listMods, "Fetched remote settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btn_reinstallTFRPlugins_Click(object sender, EventArgs e)
        { installer.installTeamSpeakPlugin(); }

        public void runInstaller(bool isLaunch)
        { installer.beginInstall(isLaunch, cfgUrl.Split('!')[1], activePack); }

        public async void runGame()
        { hideDownloadPanel(); await taskDelay(800); PrepareLaunch.LaunchGame(Arguments, this, txt_progressStatus, btn_Launch, remoteReader.ServerInfo(activePack), remoteReader.TeamSpeakInfo(), pref_joinServerAuto.Checked); }

        public void updateCurrentPack(bool refreshPacks)
        { FetchRemoteSettings(refreshPacks); GetAddons(); }

        public bool startGameAfterDownload()
        { return pref_startGameAfterDownloadsAreCompleted.Checked; }

        public bool runLauncherStartup()
        { return pref_runLauncherOnStartup.Checked; }

        public bool allowNotifications()
        { return pref_allowNotifications.Checked; }

        public bool autoDownloadUpdates()
        { return pref_autoDownload.Checked; }

        public void updateActivePack(string packName)
        { txt_selectedPack.MinimumSize = new Size(200, 0); txt_selectedPack.Text = packName; }

        public void reSizeBarText(string text)
        { txt_reSizeBar.Text = "#" + text; }

        public void showDownloadPanel()
        { botPanelIO.showPanel(); }

        public void hideDownloadPanel()
        { botPanelIO.hidePanel(); }

        private void txtb_armaDirectory_MouseClick(object sender, MouseEventArgs e)
        {
            if (txtb_armaDirectory.Text == "Set directory ->")
                txtb_armaDirectory.SelectAll();
        }

        private void txtb_armaDirectory_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            txtb_armaDirectory.SelectAll();
        }

        private void txtb_tsDirectory_MouseClick(object sender, MouseEventArgs e)
        {
            if (txtb_tsDirectory.Text == "Set directory ->")
                txtb_tsDirectory.SelectAll();
        }

        private void txtb_tsDirectory_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            txtb_tsDirectory.SelectAll();
        }

        private void txtb_modsDirectory_MouseClick(object sender, MouseEventArgs e)
        {
            if (txtb_modsDirectory.Text == "Set directory ->")
                txtb_modsDirectory.SelectAll();
        }

        private void txtb_modsDirectory_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            txtb_modsDirectory.SelectAll();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Properties.Settings.Default.firstLaunch) { Properties.Settings.Default.firstLaunch = false; Properties.Settings.Default.Save(); }
        }

        private void btn_reloadMallocs_Click(object sender, EventArgs e)
        {
            getMalloc();
        }

        private async Task taskDelay(int delayMs)
        {
            await Task.Delay(delayMs);
        }

        private void btn_cancelDownload_MouseHover(object sender, EventArgs e)
        {
            btn_cancelDownload.BackgroundImage = Properties.Resources.cloud_off_hover;
        }

        private void btn_cancelDownload_MouseLeave(object sender, EventArgs e)
        {
            btn_cancelDownload.BackgroundImage = Properties.Resources.cloud_off;
        }

        private void btn_cancelDownload_Click(object sender, EventArgs e)
        {
            if (GlobalVar.isDownloading)
            {
                if (MessageBox.Show("Are you sure you want to cancel the download progress?\nAll files will be deleted. If you want to simply pause the progress just quit the launcher and resume later on.", "Cancel download progress?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    downloader.cancelDownload();
            }

            if (GlobalVar.isInstalling)
            {
                MessageBox.Show("One does not simply cancel the installation process.", "You can't stop me now!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void pref_serverAutopilot_CheckedChanged(object sender, EventArgs e)
        {
            if (pref_serverAutopilot.Checked)
                switchAutopilot(true);
            else
                switchAutopilot(false);
        }

        private void switchAutopilot(bool On)
        {
            if (On)
            { GlobalVar.autoPilot = true; WindowVersionStatus.Text = "Autopilot engaged"; pref_serverAutopilot.Checked = true; }
            else
            { GlobalVar.autoPilot = false; WindowVersionStatus.Text = oldVersionStatusText; pref_serverAutopilot.Checked = false; }
        }

        private void WindowVersionStatus_TextChanged(object sender, EventArgs e)
        {
            if (WindowVersionStatus.Text != "Autopilot engaged")
                oldVersionStatusText = WindowVersionStatus.Text;
        }

        private void chb_battleye_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_battleye.Checked)
                GlobalVar.gameArtifact = "arma3battleye.exe";
            else
                GlobalVar.gameArtifact = "arma3.exe";
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
                windowIO.windowIn();
        }

        public void minimizeWindow()
        {
            windowIO.windowOut(false);
        }

        private void btn_checkUpdates_Click(object sender, EventArgs e)
        {
            busy.Visible = true;

            if (!QuickUpdateMethod.QuickCheck())
            {
                UpdateMethod.CheckUpdates();
                activeButton = btn_update;
                backgroundBlinker.RunWorkerAsync();
            }
        }

        private System.Windows.Forms.Timer reSize_Underline = new System.Windows.Forms.Timer();

        private async void txt_selectedPack_TextChanged(object sender, EventArgs e)
        {
            reSize_Underline.Tick += ReSizeUnderline_Tick;
            reSize_Underline.Interval = 1;

            await taskDelay(400);
            txt_selectedPack.MinimumSize = new Size(0, 0);
            await taskDelay(400);
            reSize_Underline.Start();
        }

        private void ReSizeUnderline_Tick(object sender, EventArgs e)
        {
            if (packName_underLine.Width > txt_selectedPack.Width + 30)
                packName_underLine.Width--;
            else
                packName_underLine.Width++;

            if (packName_underLine.Width == txt_selectedPack.Width + 30)
                reSize_Underline.Stop();
        }

        private void txt_thisSpace_Click(object sender, EventArgs e)
        {
            txt_thisSpace.Text = "What about now? Funky right?";
            img_thisSpace.Image = Properties.Resources.littlecat;
        }

        private void txt_thisSpace_MouseHover(object sender, EventArgs e)
        {
            if (img_thisSpace.Image == null)
            {
                Random rnd = new Random();
                int rNumber = rnd.Next(1, 10);
                switch (rNumber)
                {
                    case 1:
                        txt_thisSpace.Text = "STAY AWAY FROM ME!!!";
                        break;
                    case 2:
                        txt_thisSpace.Text = "Don't you there click me... Take that white shit out of here";
                        break;
                    case 3:
                        txt_thisSpace.Text = "Little pussy, little pussy... Come over here";
                        break;
                    case 4:
                        txt_thisSpace.Text = "I'm seeing shit in colors! Owwwww World";
                        break;
                    case 5:
                        txt_thisSpace.Text = "A blank space is not a blank space";
                        break;
                    case 6:
                        txt_thisSpace.Text = "Oh shit! What are you doing with that cursor?";
                        break;
                    case 7:
                        txt_thisSpace.Text = "What da'Hell? This text changes!! *MAGIC*";
                        break;
                    case 8:
                        txt_thisSpace.Text = "*PUFF*";
                        break;
                    case 9:
                        txt_thisSpace.Text = "This is absolute shit (period)";
                        break;
                    case 10:
                        txt_thisSpace.Text = "There are 128 different colors in that cat. I know it because I saw him.";
                        break;
                    default:
                        break;
                }
            }
        }

        private void txt_thisSpace_MouseLeave(object sender, EventArgs e)
        {
            if (img_thisSpace.Image == null)
            {
                txt_thisSpace.Text = "Does this blank space bother you?";
            }
        }
    }
}