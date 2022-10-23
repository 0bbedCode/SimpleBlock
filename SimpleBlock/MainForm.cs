using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using SimpleBlock.ThreadEx;

namespace SimpleBlock {
    public partial class MainForm : Form {
        private readonly RepoConfig _config;
        private readonly HostFile _host;
        private readonly AllowedHosts _allowed;

        public readonly CancelToken TokenCancel = new CancelToken();

        private UnsafeThread _backGroundworker = null;
        private UnsafeThread _updateCheckThread = null;
        private UnsafeThread _updatingThread = null;

        private bool _hasUpdate = false;
        private bool _hasInitRepoTab = false;
        private bool _writeRepoAfterUpdate = false;

        public MainForm(string hostFile) {
            InitializeComponent();

            RepoCore.OnNewRepo += OnNewRepoHandler;
            RepoCore.OnAddRepo += OnAddRepoHandler;
            RepoCore.OnRemRepo += OnRemoveRepoHandler;
            LogEx.OnLog += OnAppendLogHandler;

            if (Utils.IsDNSCacheEnabled()) 
                LogEx.LogError("DNS Cache is Enabled", "DNS Cache Service is Still Enabled. Please Disable then restart the PC to make sure this Tool works best");
            
            _config = new RepoConfig(Utils.RepoConfig);
            LogEx.LogData($"Initialized Repo Config", Utils.RepoConfig);
            _allowed = new AllowedHosts(Utils.AllowedConfig);
            LogEx.LogData("Initialized Allowed File", hostFile);
            _host = new HostFile(hostFile, _allowed);
            LogEx.LogData("Initialized Host File", hostFile);

            InitRepos();
            InitAllowed();
            InitUISettings();
            InitBackgroundUpdaterEx();
        }

        private void CheckForUpdates(bool force = false) {
            if (RepoCore.IsCheckingForUpdate || _updatingThread != null || _host.IsUpdating) {
                LogEx.LogCoreIsBusy($"IsCheckingForUpdate={RepoCore.IsCheckingForUpdate} >> ThreadIsNull={_updatingThread is null} >> IsUpdating={_host.IsUpdating}");
                return;
            }

            updateBox.Style = ProgressBarStyle.Marquee;
            _updateCheckThread = TokenCancel.StartAsyncEx(() => { _hasUpdate = RepoCore.HasUpdate(_host, force); }, OnEndedProgressThreadHandler);
        }

        public void UpdateCoreIdle(bool enabled) {
            repoList.Enabled = enabled;
            allowedList.Enabled = enabled;
            hostList.Enabled = enabled;
            
            checkUpdatesButton.Enabled = enabled;
            
            repoCheckChar.Enabled = enabled;
            repoCheckTime.Enabled = enabled;
            repoCheckDeep.Enabled = enabled;

            TokenCancel.StartAsync(() => {
                if (!Enabled) UpdateTotalBlockedHandler(0);
                else {
                    _host.InitHost(true);
                    UpdateTotalBlockedHandler(_host.BlockedHosts.Count);
                }
            });
        }

        #region Init Functions
        public void InitAllowed() {
            foreach(var itm in _allowed.Allowed) {
                var lvi = new ListViewItem { Text = itm };
                if (InvokeRequired) this.BeginInvoke(new Action(() => { allowedList.Items.Add(lvi); }));
                else allowedList.Items.Add(lvi);
            }
        }

        public void InitRepos() {
            LogEx.LogData("Updating UI Repo", "Updating Repo List View to Match Repo Config");
            foreach (var repo in _config.EnumRepos()) {
                if (!RepoCore.HasRepo(repo))
                    RepoCore.AddExistingRepo(repo);
            }
        }

        public void InitUISettings() {
            LogEx.LogData("Updating UI Settings", "Updating UI to Match Settings Config");
            repoCheckChar.Checked = AppSettings.RepoCheckChar; //AppSettings.Get<bool>("RepoCheckChar");//RunBackgroundUpdater
            repoCheckTime.Checked = AppSettings.RepoCheckTime;
            repoCheckDeep.Checked = AppSettings.RepoCheckDeep; //AppSettings.Get<bool>("RepoCheckDeep");//DeepUpdateCheck
            PauseResumeButton.Text = AppSettings.IsPaused ? "Resume" : "Pause";
            UpdateCoreIdle(!AppSettings.IsPaused);
        }

        public void InitBackgroundUpdaterEx() {
            if (AppSettings.RunBackgroundUpdates) {
                if (_backGroundworker != null)
                    return;

                _backGroundworker = TokenCancel.StartAsyncEx(BackgroundWorkerEx);
            }
            else {
                if (_backGroundworker is null)
                    return;

                _backGroundworker.Dispose();
                _backGroundworker = null;
            }
        }
        #endregion

        public void BackgroundWorkerEx() {
            LogEx.LogData("Background Worker Working", "Background Update Checker Working In the Background");
            while (!AppSettings.RunBackgroundUpdates) {//RunBackgroundUpdater
                Thread.Sleep(AppSettings.Get<int>("SleepCheck"));
                if (_hasUpdate)
                    continue;

                if (AppSettings.IsPaused)
                    return;

                if (_host.IsUpdating || RepoCore.IsCheckingForUpdate)
                    continue;

                _hasUpdate = RepoCore.HasUpdate(_host);
            }
        }

        public void UpdateListView() {
            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating}");
                return;
            }

            if (_host.BlockedHosts.Count == 0)
                return;

            var res = MessageBox.Show("Do you want to Update Host ListView ? (This may take a while)", "ListView Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes)
                return;

            hostList.Enabled = false;
            hostList.Items.Clear();

            foreach (var itm in _host.EnumBlocked())
                hostList.Items.Add(new ListViewItem { Tag = itm, Text = itm.Host });

            hostList.Enabled = true;
        }

        public void StopUpdateCheckAsync() {
            if (_updateCheckThread is null)
                return;

            TokenCancel.StartAsync(() => { _updateCheckThread.Stop(false, true, false); }, OnEndedProgressThreadHandler);
        }

        public void RestoreFromCopy(string file) {
            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating}");
                return;
            }

            TokenCancel.StartAsync(() => {
                _host.IsUpdating = true;
                _host.RestoreFromCopy(file);
                _host.InitHost();
                UpdateTotalBlockedHandler(_host.BlockedHosts.Count);
                _host.IsUpdating = false;
            });
        }

        #region Handlers
        public void OnRemoveRepoHandler(Repo repo) {
            foreach(ListViewItem itm in repoList.Items) {
                var rep = (Repo)itm.Tag;
                if (rep == repo) {
                    repoList.Items.Remove(itm);
                    break;
                }
            }

            _config.WriteRepos(RepoCore.Repos);
        }

        public void OnAppendLogHandler(LogEx log) {
            var lvi = new ListViewItem { Text = log.GetTypeLogName() };
            lvi.SubItems.Add(log.ShortMessage);
            lvi.SubItems.Add(log.TimeStamp.ToString());
            lvi.Tag = log;
            if (InvokeRequired) this.BeginInvoke(new Action(() => { logList.Items.Add(lvi); }));
            else logList.Items.Add(lvi);
        }

        public void OnAddRepoHandler(Repo repo) {
            var lvi = new ListViewItem { Text = repo.Name };
            lvi.SubItems.Add(repo.URI);
            lvi.Checked = repo.Enabled;
            lvi.Tag = repo;
            if (InvokeRequired) this.BeginInvoke(new Action(() => { repoList.Items.Add(lvi); }));
            else repoList.Items.Add(lvi);
        }

        public void OnNewRepoHandler(Repo repo) {
            OnAddRepoHandler(repo);
            _config.WriteRepos(RepoCore.Repos);
        }

        public void OnEndedProgressThreadHandler(UnsafeThread thread) {
            updateBox.Invoke(new Action(() => { updateBox.Style = ProgressBarStyle.Continuous; }));
            RepoCore.IsCheckingForUpdate = false;
            _updateCheckThread = null;
        }

        public void OnAllowedEnded(UnsafeThread thread) {
            _host.InitHost(true);
            UpdateTotalBlockedHandler(_host.BlockedHosts.Count);
            _host.IsUpdating = false;
        }

        public void OnUpdateEndedHandler(UnsafeThread thread) {
            TokenCancel.StartAsync(() => {
                if (_writeRepoAfterUpdate) {
                    _config.WriteRepos(RepoCore.Repos, true);
                    _writeRepoAfterUpdate = false;
                }

                _host.InitHost(true);
                UpdateTotalBlockedHandler(_host.BlockedHosts.Count);
                updateBox.Invoke(new Action(() => { updateBox.Style = ProgressBarStyle.Continuous; }));
                _updatingThread = null;
                _host.IsUpdating = false;
            });
        }

        #endregion

        private void AddToolStripMenuItem1_Click(object sender, EventArgs e) {
            var newRepo = new RepoModifyForm();
            newRepo.Show();
        }

        private void RemoveToolStripMenuItem1_Click(object sender, EventArgs e) {
            foreach (ListViewItem itm in repoList.SelectedItems) {
                repoList.Items.Remove(itm);
                RepoCore.RemoveRepo((Repo)itm.Tag);
            }

            _config.WriteRepos(RepoCore.Repos);
        }

        private void RepoList_ItemChecked(object sender, ItemCheckedEventArgs e) {
            if (!_hasInitRepoTab)
                return;

            var obj = (Repo)e.Item.Tag;
            obj.Enabled = e.Item.Checked;
            _hasUpdate = true;
            _config.WriteRepos(RepoCore.Repos);
            LogEx.LogData("Core Update", "Repo has been Enabled to the Targeted Blocked Hosts, Update the Core to Apply Changes to the Host File");
        }

        private void RefreshToolStripMenuItem_Click(object sender, EventArgs e)
            => UpdateListView();

        public void UpdateTotalBlockedHandler(int blocked) {
            if (InvokeRequired) this.BeginInvoke(new Action(() => { totalBlocked.Text = blocked.ToString(); }));
            else totalBlocked.Text = blocked.ToString();
            //if (!_hasInitUi) {
            //    //totalBlocked.Invoke(new Action(() => { totalBlocked.Text = blocked.ToString(); }));
            //    //totalBlocked.Text = blocked.ToString();
            //    //this.BeginInvoke(new Action(() => { totalBlocked.Text = blocked.ToString(); }));
            //}
            //else {
            //    totalBlocked.Invoke(new Action(() => { totalBlocked.Text = blocked.ToString(); }));
            //}
        }

        private void ClearHostsToolStripMenuItem_Click_1(object sender, EventArgs e) {
            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating}");
                return;
            }

            if (MessageBox.Show("Are you sure you want to clear out your hosts File?", "Hosts", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            hostList.Items.Clear();
            _host.Clear();
            UpdateTotalBlockedHandler(0);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e) {
            if (logList.SelectedItems.Count != 1)
                return;

            var itm = (LogEx)logList.SelectedItems[0].Tag;
            var lgfrm = new LogMsgForm(itm);
            lgfrm.Show();
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e) {
            if (logList.Items.Count == 0)
                return;

            logList.Items.Clear();
            logList.Enabled = false;
            foreach (ListViewItem itm in logList.Items) {
                var log = (LogEx)itm.Tag;
                log.Dispose();
            }

            logList.Items.Clear();
            logList.Enabled = true;
        }

        private void LogList_MouseDoubleClick(object sender, MouseEventArgs e) {
            if (logList.SelectedItems.Count == 1) {
                var itm = (LogEx)logList.SelectedItems[0].Tag;
                var lgfrm = new LogMsgForm(itm);
                lgfrm.Show();
            }
        }

        private void RepoCheckCharCheck_CheckedChanged(object sender, EventArgs e) {
            AppSettings.RepoCheckChar = repoCheckChar.Checked;
            InitBackgroundUpdaterEx();
        }

        private void RepoCheckTimeCheck_CheckedChanged(object sender, EventArgs e) {
            AppSettings.RepoCheckTime = repoCheckTime.Checked;
            InitBackgroundUpdaterEx();
        }

        private void RepoCheckDeepCheck_CheckedChanged(object sender, EventArgs e) {
            var res = MessageBox.Show("This is a Beta Check, Use it if you dont mind alot of resources being used on your PC. Its a Non Needed Check. Do you still want this?", "Are you Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if(res == DialogResult.Yes) {
                AppSettings.RepoCheckDeep = repoCheckDeep.Checked;
                InitBackgroundUpdaterEx();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            Program.Mutex.Close();
            Process.GetCurrentProcess().Kill();
        }

        private void CheckUpdatesButton_Click(object sender, EventArgs e) 
            => CheckForUpdates(true);

        private void StartToolStripMenuItem_Click(object sender, EventArgs e) {
            if (_host.IsUpdating || _updatingThread != null) {//not equal to null check or ovveride it ?
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating} >> UpdatingThreadIsNULL={_updatingThread is null}");
                return;
            }

            updateBox.Style = ProgressBarStyle.Marquee;
            _updatingThread = TokenCancel.StartAsyncEx(() => { _writeRepoAfterUpdate = _host.UpdateHostsEx(); }, OnUpdateEndedHandler);
        }

        private void StopToolStripMenuItem_Click(object sender, EventArgs e) {
            if (_updatingThread is null)
                return;

            TokenCancel.StartAsync(() => { 
                _updatingThread.Stop(false, true, false); 
                _host.RestoreFromCopy(_host.BackupHost);
            }, OnUpdateEndedHandler);
        }

        private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e) {
            if (mainTabControl.SelectedTab == mainTabControl.TabPages["tabPage2"] && !_hasInitRepoTab)
                _hasInitRepoTab = true;
        }

        private void MainForm_Shown(object sender, EventArgs e) {
        }

        private void PauseResumeButton_Click(object sender, EventArgs e) {//lock this ?
            if (_host.IsUpdating || RepoCore.IsCheckingForUpdate) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating} >> IsCheckingForUpadte={RepoCore.IsCheckingForUpdate}");
                return;
            }

            if (!AppSettings.IsPaused) {
                if (_host.CreateCopy(true)) {
                    AppSettings.IsPaused = true;
                    PauseResumeButton.Text = "Resume";
                    UpdateCoreIdle(false);
                }
            }
            else {
                if (_host.RestoreFromCopy(_host.BackupHost)) {
                    AppSettings.IsPaused = false;
                    PauseResumeButton.Text = "Pause";
                    UpdateCoreIdle(true);
                }
            }
        }

        private void DisableDNSButton_Click(object sender, EventArgs e) 
            => Utils.SetDNSCacheState(false);

        private void EnableDNSCache_Click(object sender, EventArgs e) 
            => Utils.SetDNSCacheState(true);

        private void ClearDNSButton_Click(object sender, EventArgs e)
            => Interaction.Shell("ipconfig /flushdns");

        private void CheckForUpdateToolStripMenuItem_Click(object sender, EventArgs e) 
            => CheckForUpdates(true);

        private void StopUpdateCheckToolStripMenuItem_Click(object sender, EventArgs e) {
            StopUpdateCheckAsync();
        }

        private void StartToolStripMenuItem1_Click(object sender, EventArgs e) {
            if (_host.IsUpdating || _updatingThread != null) {//not equal to null check or ovveride it ?
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating} >> UpdatingThreadIsNull{_updatingThread is null}");
                return;
            }

            var itms = repoList.SelectedItems;
            if (itms.Count == 0)
                return;

            var lst = itms.Cast<ListViewItem>().Select(i => (Repo)i.Tag).ToList();
            if (lst is null || lst.Count == 0)
                return;

            updateBox.Style = ProgressBarStyle.Marquee;
            _updatingThread = TokenCancel.StartAsyncEx(() => { _writeRepoAfterUpdate = _host.UpdateViaRepos(lst); }, OnUpdateEndedHandler);
        }

        private void StopToolStripMenuItem1_Click(object sender, EventArgs e) {
            if (_updatingThread is null)
                return;

            TokenCancel.StartAsync(() => {
                _updatingThread.Stop(false, true, false);
                _host.RestoreFromCopy(_host.BackupHost);
            }, OnUpdateEndedHandler);
        }

        private void OpenHostFolderButton_Click(object sender, EventArgs e) 
            => Process.Start(new FileInfo(_host.Path).DirectoryName);

        private void OpenHostButton_Click(object sender, EventArgs e)
            => Process.Start($"notepad", _host.Path);

        private void Convert02127_Click(object sender, EventArgs e) {
            if(_host.IsUpdating || AppSettings.IsPaused) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating} >> IsPaused={AppSettings.IsPaused}");
                return;
            }

            TokenCancel.StartAsync(() => { _host.ConvertRedirects("0.0.0.0", "127.0.0.1"); });
        }

        private void EditToolStripMenuItem_Click(object sender, EventArgs e) {
            if (repoList.SelectedItems.Count != 1)
                return;

            var selected = repoList.SelectedItems[0];
            var repModify = new RepoModifyForm((Repo)selected.Tag);
            repModify.Show();
        }

        private void DefaultToolStripMenuItem_Click(object sender, EventArgs e)
            => RestoreFromCopy(_host.BackupHost);

        private void CustomToolStripMenuItem_Click(object sender, EventArgs e) {
            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating}");
                return;
            }

            var ofdlg = new OpenFileDialog { Title = "Open Backup Host File" };
            if(ofdlg.ShowDialog() == DialogResult.OK) {
                TokenCancel.StartAsync(() => {
                    var lns = ParserUtils.ParseFromFile(ofdlg.FileName);
                    if (lns.Count == 0) 
                        return;

                    RestoreFromCopy(ofdlg.FileName);
                });
            }
        }

        private void CustomToolStripMenuItem1_Click(object sender, EventArgs e) {
            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating}");
                return;
            }

            var svdlg = new SaveFileDialog { Title = "Save Host Backup" };
            if (svdlg.ShowDialog() == DialogResult.OK) {
                _host.IsUpdating = true;
                File.Copy(_host.Path, svdlg.FileName);
                _host.IsUpdating = false;
            }
        }

        private void DefaultToolStripMenuItem1_Click(object sender, EventArgs e) {
            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating}");
                return;
            }

            _host.IsUpdating = true;
            _host.CreateCopy(false);
            _host.IsUpdating = false;
        }

        private void Convert12720_Click(object sender, EventArgs e) {
            if (_host.IsUpdating || AppSettings.IsPaused) {
                LogEx.LogCoreIsBusy($"IsUpdating={_host.IsUpdating} >> IsPaused={AppSettings.IsPaused}");
                return;
            }

            TokenCancel.StartAsync(() => { _host.ConvertRedirects("127.0.0.1", "0.0.0.0"); });
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => Process.Start("https://github.com/0bbedCode");

        private void RepoCharChecksCheck_MouseHover(object sender, EventArgs e) 
            => repoCharTip.Show("Compares Char Length Cache with Char Length Downloaded", repoCheckChar);

        private void RepoDeepChecksCheck_MouseHover(object sender, EventArgs e) 
            => repoDeepTip.Show("Compares Downloaded Enteries with Host Enteries", repoCheckDeep);

        private void RepoCheckTime_MouseHover(object sender, EventArgs e) 
            => repoTimeTip.Show("Compares Last Update TimeStamps", repoCheckTime);


        private void ChechDNSCacheButton_Click(object sender, EventArgs e)
            => MessageBox.Show($"DNS Cache Is Enabled: {Utils.IsDNSCacheEnabled()}", "DNS Cache State", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void AddToolStripMenuItem2_Click(object sender, EventArgs e) {
            var inp = Interaction.InputBox("Enter URL to allow", "URL");
            if (!Utils.StringIsValid(inp) || _allowed.Allowed.Contains(inp))
                return;

            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy("IsBusy");
                return;
            }

            _host.IsUpdating = true;

            var ops = ParserUtils.FindOp(inp);
            _allowed.Allowed.Add(inp);
            allowedList.Items.Add(new ListViewItem() { Text = inp });
            _allowed.Allowed.Add(ops);
            allowedList.Items.Add(new ListViewItem() { Text = ops });

            _allowed.WriteAllowed();
            TokenCancel.StartAsync(() => { _host.UpdateAllowed(); }, OnAllowedEnded);
        }

        private void RemoveToolStripMenuItem2_Click(object sender, EventArgs e) {
            var itms = allowedList.SelectedItems;
            if (itms is null || itms.Count == 0)
                return;

            if (_host.IsUpdating) {
                LogEx.LogCoreIsBusy("IsBusy");
                return;
            }

            _host.IsUpdating = true;
            foreach (ListViewItem itm in itms) {
                var op = ParserUtils.FindOp(itm.Text);
                foreach(ListViewItem itm2 in allowedList.Items) {
                    if(itm2.Text == op) {
                        _allowed.Allowed.Remove(itm2.Text);
                        allowedList.Items.Remove(itm2);
                    }
                }

                _allowed.Allowed.Remove(itm.Text);
                allowedList.Items.Remove(itm);
            }

            _allowed.WriteAllowed();
            TokenCancel.StartAsync(() => { _host.UpdateAllowed(); }, OnAllowedEnded);
        }
    }
}
