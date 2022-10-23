using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SimpleBlock {
    public class HostFile : SimpleIO {
        public List<Entry> BlockedHosts = new List<Entry>();
        public List<Entry> RedirectedHosts = new List<Entry>();

        public AllowedHosts Allowed;
        public string BackupHost;
        public bool IsUpdating = false;

        public HostFile(string path, AllowedHosts allowed) : base(path, false) { InitHost(); BackupHost = new FileInfo(Path).Directory + "\\Host-Backup"; Allowed = allowed; }

        public bool UpdateAllowed() {
            lock (this) {
                try {
                    LogEx.LogHost("Filtering File", "Updating and Filtering out allowed Hosts in the Host File");
                    var data = Allowed.FilterEx(ParserUtils.ParseFromFile(Path));
                    Clear();
                    WriteBlocked(data);
                    return true;
                }
                catch(Exception e) {
                    LogEx.LogError("Update Failed", e);
                    return false;
                }
            }
        }

        public bool RestoreFromCopy(string file) {//Lock
            try {
                Close();
                LogEx.LogHost("Restoring Hosts", $"Restoring Host File From Backup >> {file} >> {Path}");
                if (!File.Exists(file)) {
                    LogEx.LogError("Backup Host Does not Exist", $"Backup Host File does not exist to restore >> {file}");
                    return false;
                }

                File.Copy(file, Path, true);
                LogEx.LogHost("Restoring Hosts Finished", $"Host File is Done Restoring >> {Path}");
                return true;
            }
            catch(Exception e) { LogEx.LogError("Failed to Restore Host", e); return false; }
        }

        public bool CreateCopy(bool replaceEmpty) {
            try {
                Close();
                File.Copy(Path, BackupHost, true);
                LogEx.LogHost("Copied Host", "Made a Host File Backup / Copy");
                if (replaceEmpty) 
                    Clear();

                return true;
            }catch(Exception e) { LogEx.LogError("Failed Copying Host", e); return false; }
        }

        public bool UpdateViaRepos(List<Repo> repos) {
            try {
                if (IsUpdating) {
                    LogEx.LogCoreIsBusy($"IsUpdating={IsUpdating}");
                    return false;
                }

                if(repos is null || repos.Count == 0) {
                    LogEx.LogError("Repos is NULL", $"Repos List is Null or Empty");//make a LogLog
                    return false;
                }

                IsUpdating = true;
                LogEx.LogHost("Pushing Host Update", $"Pushing Repos to the Host File >> {Path}");
                var enters = RepoCore.GetAllEnteries(repos, true, true);
                if (enters is null) {
                    LogEx.LogError("Enteries is NULL");
                    return false;
                }

                var lst = new List<Entry>();
                LogEx.LogHost("Filtering Duplicates", $"Removing Duplicate Hosts from Enteries >> {enters.Count}");

                for(var i = 0; i < enters.Count; i++) {
                    var entery = enters[i];

                    var found = false;
                    for(var ih = 0; ih < RedirectedHosts.Count; ih++) {
                        if (RedirectedHosts[i].Host == entery.Host) {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        lst.Add(entery);
                }

                if (lst.Count == 0)
                    return false;

                return WriteBlocked(lst);

            }catch(Exception e) { LogEx.LogError("Error Updating Direct Repos", e); }
            finally { IsUpdating = false; }
            return false;
        }

        public bool UpdateHostsEx() {
            try {
                if (IsUpdating) {
                    LogEx.LogCoreIsBusy($"IsUpdating={IsUpdating}");
                    return false;
                }

                LogEx.LogHost("Update Starting", $"Starting Host File Update >> {Path}");
                IsUpdating = true;
                var stpwatch = new Stopwatch();
                stpwatch.Start();

                CreateCopy(false);
                WriteRedirected();
                Clear();

                if (RepoCore.HasNoEnabledRepos()) 
                    return true;
                
                var enteries = RepoCore.GetAllEnteries(true, true);
                if (enteries is null || enteries.Count == 0) {
                    LogEx.LogIsNULL("UpdateHostsEx.Enteries");
                    return false;
                }

                if (!WriteBlocked(enteries)) 
                    return false;
                
                //Init Here ?
                stpwatch.Stop();
                LogEx.LogHost("Update Finished", $"Finished >> {stpwatch.ElapsedMilliseconds} >> {TimeSpan.FromMilliseconds(stpwatch.ElapsedMilliseconds).TotalMinutes}");
                return true;
            }
            catch (Exception e) {
                LogEx.LogError("Core Update Failed", e);
                return false;
            }
            finally {
                IsUpdating = false;
            }
        }

        public void ConvertRedirects(string direct, string newDirect) {
            try {
                LogEx.LogHost($"Converting Redirects", $"Converting {direct} => {newDirect}");
                IsUpdating = true;
                var lns = File.ReadAllLines(Path);
                var lst = new List<string>(lns.Length);
                for (var i = 0; i < lns.Length; i++) {
                    var itm = lns[i];
                    if (itm.StartsWith($"{direct}")) {//make sure space is included
                        var newln = itm.Substring(direct.Length + 1);
                        lst.Add(newDirect + "§" + newln);
                        continue;
                    }

                    lst.Add(itm);
                }

                Clear();
                WriteRedirected();
                WriteBlocked(lst);
            }
            catch (Exception ex) { LogEx.LogIOException("Failed to Convert Host File", ex); RestoreFromCopy(BackupHost); }
            finally { IsUpdating = false; }
        }


        public bool WriteBlocked(List<Entry> enteries) {
            try {
                if (enteries is null || enteries.Count == 0) {
                    LogEx.LogIsNULL("enteries");
                    return false;
                }

                if (!Open())
                    return false;

                LogEx.LogHost($"Writing Blocked Enteries", $"Writing All Blocked Enteries >> {Path} >> {enteries.Count}");

                for (var i = 0; i < enteries.Count; i++) {
                    var entry = enteries[i];
                    _writer.WriteLine(entry.ToString());
                    BlockedHosts.Add(entry);
                }

                Close();
                LogEx.LogHost("Finished Block Enteries", $"Finished Writing All Blocked Enteries >> {Path} >> {enteries.Count}");
                return true;
            }
            catch (Exception e) { LogEx.LogError("WriteBlocked Hosts Error", e); }
            return false;
        }

        public bool WriteBlocked(List<string> blocked) => WriteBlocked(blocked.ToArray()); 
        public bool WriteBlocked(string[] blocked) {
            //make sure every call also writes redirects
            //Also sort enteries by Comment so its orginized
            try {
                if (blocked is null || blocked.Length == 0) {
                    LogEx.LogIsNULL("Blocked");
                    return false;
                }

                if (!Open())
                    return false;

                LogEx.LogHost($"Writing Blocked Enteries", $"Writing All Blocked Enteries >> {Path} >> {blocked.Length}");

                for(var i = 0; i < blocked.Length; i++) {
                    if (!Utils.StringIsValid(blocked[i])) 
                        continue;
                    
                    var entry = new Entry(blocked[i]);
                    _writer.WriteLine(entry.ToString());
                    BlockedHosts.Add(entry);
                }

                Close();
                LogEx.LogHost("Finished Block Enteries", $"Finished Writing All Blocked Enteries >> {Path} >> {blocked.Length}");
                return true;
            }
            catch(Exception e) { LogEx.LogError("WriteBlocked Hosts Error", e); }
            return false;
        }

        public bool WriteRedirected() {
            try {
                InitHost(false);//need ?
                if(RedirectedHosts.Count == 0) {
                    LogEx.LogIsNULL("WD.RedirectedHosts");
                    return true;
                }

                if (!Open())
                    return false;

                LogEx.LogHost("Writing Redirects", $"Writing Redirects to the Host File >> {Path}");
                for (var i = 0; i < RedirectedHosts.Count; i++)
                    _writer.WriteLine(RedirectedHosts[i].ToString());

                Close();
            }
            catch(Exception e) { LogEx.LogError("Failed to Write Redirected Hosts", e); }
            return false;
        }

        public IEnumerable<Entry> EnumBlocked()
            => BlockedHosts;

        public IEnumerable<Entry> EnumRedirected()
            => RedirectedHosts;

        public bool InitHost(bool includeBlocked = true) {
            lock (this) {
                BlockedHosts.Clear();//maybe do if check true
                RedirectedHosts.Clear();

                LogEx.LogHost("Init Headers", $"Parsing Host Headers / Enteries >> {Path}");
                var hosts = ParserUtils.ParseFromFile(Path);
                if (hosts is null) {
                    LogEx.LogIsNULL($"InitHost.hosts", Path);
                    return false;
                }

                for (var i = 0; i < hosts.Count; i++) {
                    var itm = hosts[i];
                    if (itm.IsBlocking() && includeBlocked) BlockedHosts.Add(itm);
                    else RedirectedHosts.Add(itm);
                }

                LogEx.LogHost("Init Headers Finished");
                return true;
            }
        }
    }
}
