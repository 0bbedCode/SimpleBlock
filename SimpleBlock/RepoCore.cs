using System;
using System.Collections.Generic;

namespace SimpleBlock {
    public delegate void repoaction_del(Repo repo);

    public class RepoCore {
        public static event repoaction_del OnNewRepo;
        public static event repoaction_del OnRemRepo;

        public static event repoaction_del OnAddRepo;

        public static event repoaction_del OnEnbRepo;
        public static event repoaction_del OnDisRepo;

        public static bool IsCheckingForUpdate = false;

        public static List<Repo> Repos = new List<Repo>();
        public static AllowedHosts Allowed;

        public static void EnableRepo(Repo repo)
            => OnEnbRepo?.Invoke(repo);

        public static void DisableRepo(Repo repo)
            => OnDisRepo?.Invoke(repo);

        public static bool HasNoEnabledRepos() {
            if (Repos.Count > 0)
                return false;

            LogEx.LogRepoEvent("Checking Enabled Repos", string.Empty);

            foreach (var repo in Repos.ToArray())
                if (repo.Enabled)
                    return false;

            LogEx.LogError("No Enabled Repos", "No Repos Enabled to Get Any Enteries from");
            return true;
        }

        public static bool HasUpdate(HostFile host, bool force = false) {
            try {
                if (IsCheckingForUpdate) {
                    LogEx.LogCoreIsBusy($"IsCheckingForUpdate={IsCheckingForUpdate}");
                    return false;
                }

                IsCheckingForUpdate = true;
                LogEx.LogRepoEvent("Checking For Updates", "Checking If Repo Core has An Update");
                if (AppSettings.RepoCheckDeep)
                    host.InitHost();

                if (Repos.Count == 0 || HasNoEnabledRepos()) {
                    LogEx.LogNoUpdatesFound("No Enabled Repos");
                    return false;
                }

                foreach (var repo in Repos) {
                    if (!repo.Enabled)
                        continue;

                    if (AppSettings.RepoCheckChar || (!AppSettings.RunBackgroundUpdates && force)) {
                        LogEx.LogRepoEvent("Char Repo Check", "Performing a Repo Check using Char Count Comparing");
                        var data = ParserUtils.DownloadRepoText(repo.URI);//Fix Later to be more optmized
                        if (data is null) {
                            LogEx.LogIsNULL("HasUpdate.data", $"Repo >> {repo.ToStringEx()} >> Failed to DownloadRepoText <RepoCore.HasUpdate>");
                            return false;
                        }

                        if (data.Length != repo.Count) {
                            LogEx.LogNeedsUpdate($"Repo >> {repo.ToStringEx()}New Count: {data.Length}");
                            return true;
                        }
                    }

                    if (AppSettings.RepoCheckTime) {
                        LogEx.LogRepoEvent("Time Repo Check", "Performing a Repo Check using TimeStamp Comparing");
                        if (repo.HasUpdatedTimeStamp()) {
                            LogEx.LogNeedsUpdate($"Repo >> {repo.ToStringEx()}");
                            return true;
                        }
                    }

                    if (!AppSettings.RepoCheckDeep)
                        continue;

                    LogEx.LogRepoEvent("Deep Repo Check", "Performing a Deep Repo Check, Gathering Enteries with the URL Compare to Host File");

                    var blcked = repo.GetEnteries();
                    if (blcked is null) {
                        LogEx.LogIsNULL("HasUpdate.blcked", $"Repo >> {repo.ToStringEx()} >> Failed to Grab Enteries <RepoCore.HasUpdate>");
                        return false;
                    }

                    foreach (var entry in blcked) {//fix
                        var found = false;
                        foreach (var hst in host.BlockedHosts) {
                            if (entry == hst.Host) {
                                found = true;
                                break;
                            }

                            if (found)
                                continue;

                            LogEx.LogNeedsUpdate($"Host File is Missing Repo Enteries :L >> {repo.ToStringEx()}");
                            return true;
                        }
                    }
                }

                LogEx.LogNoUpdatesFound();
                return false;

            }
            catch (Exception e) {
                LogEx.LogError("Failed to Check RepoCore Updates", e);
                return false;
            }
            finally {
                IsCheckingForUpdate = false;
            }
        }

        public static List<Entry> GetAllEnteries(bool setCount = false, bool setTime = false) => GetAllEnteries(Repos);
        public static List<Entry> GetAllEnteries(List<Repo> repos, bool setCount = false, bool setTime = false) {
            if (repos is null || repos.Count == 0) {
                LogEx.LogIsNULL($"GetAllEnteries.Repos");
                return null;
            }

            LogEx.LogRepoEvent("Starting Entery Enum", $"Grabbing all Repo Enteries from Enabled Repos");
            try {
                var hshItems = new HashSet<string>();
                foreach (var repo in repos.ToArray()) {
                    if (repo.Enabled) {
                        var entrs = repo.GetEnteries(setCount, setTime);
                        LogEx.LogRepoEvent("Sorting Enteries", $"Repo >> {repo.ToStringEx()}Entery Count: {entrs.Count}");
                        foreach (var itm in entrs)
                            hshItems.Add(itm);
                    }
                }

                LogEx.LogRepoEvent("Merging Enteries Finished", $"Finished Merging all Enteries into one Array >> {hshItems.Count} Now Converting to Entery Array");

                var entries = new List<Entry>(hshItems.Count);
                foreach(var itm in hshItems)
                    entries.Add(new Entry(itm));

                return entries;
            }
            catch (Exception e) { LogEx.LogError("Error Getting Enteries", e); }
            return null;
        }

        public static void AddExistingRepo(Repo repo) {
            if (repo is null || HasRepo(repo))
                return;

            Repos.Add(repo);
            OnAddRepo?.Invoke(repo);
        }

        public static void AddNewRepo(Repo repo, bool invokeHandler = true) {//lock these ?
            if (repo is null || HasRepo(repo))
                return;

            Repos.Add(repo);
            if (invokeHandler)
                OnNewRepo?.Invoke(repo);
        }

        public static bool HasRepo(Repo repo) {
            //foreach(var rep in Repos.ToArray()) {
            //    if (rep.Name == repo.Name || rep.URI == repo.URI)
            //        return true;
            //}

            //return false;
            return Repos.Contains(repo);
        }

        public static void RemoveRepo(Repo repo) {
            if (repo is null || !HasRepo(repo))
                return;

            Repos.Remove(repo);
            OnRemRepo?.Invoke(repo);
        }
    }
}
