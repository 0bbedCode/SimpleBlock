using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleBlock {
    public class RepoConfig : SimpleIO {
        public RepoConfig(string path) : base(path) { }
        public void WriteRepos(List<Repo> repos, bool updateRepoTime = false) {
            lock (this) {
                if(repos is null || repos.Count == 0) {
                    LogEx.LogIsNULL($"WriteRepos.Repos");
                    return;
                }

                LogEx.LogRepoConfig("Writing To Config", $"Writing all Repos >> {Path}");
                var tm = DateTime.Now;
                Clear();
                Open();
                foreach (var itm in repos.ToArray()) {
                    _writer.WriteLine(itm.ToString());
                    if (updateRepoTime)
                        itm.LastUpdate = tm;
                }

                Close();
            }
        }
        
        public void WriteRepo(Repo repo) {
            lock (this) {
                try {
                    Close();
                    LogEx.LogRepoConfig("Writing To Config", $"Writing Repo To Config File >> {Path}\n{repo.ToStringEx()}");
                    File.AppendAllText(Path, repo.ToString());
                }
                catch(Exception e) { LogEx.LogError("WriteRepo ERROR", e); }
            }
        }

        public IEnumerable<Repo> EnumRepos() {
            Close();
            var lns = TryReadAllLines();
            if (lns is null || lns.Length == 0)
                yield break;

            LogEx.LogRepoConfig("Enuming Repo Config", $"{lns.Length} >> {Path}");

            foreach (var ln in lns) {
                if (!Utils.StringIsValid(ln) || !ln.Contains("§"))
                    continue;

                if (new Regex("§").Matches(ln).Count == 5) 
                    yield return new Repo(ln);
            }

        }
    }
}
