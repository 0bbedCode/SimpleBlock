using System;
using System.IO;
using System.Collections.Generic;

namespace SimpleBlock {
    public class Repo {
        public bool IsFile;
        public string URI;
        public string Name;
        public bool Enabled = false;

        public int Count = 0;

        private bool _hasUpdate = false;

        public DateTime LastUpdate;
        public GithubURL GitURL = null;

        public Repo(string data) {
            var splt = data.Split('§');
            Name = splt[0];
            URI = splt[1];
            if (GithubURL.IsHostedOnGit(URI) || URI.Contains("github.com"))
                GitURL = new GithubURL(URI);

            IsFile = Convert.ToBoolean(splt[2]);
            Enabled = Convert.ToBoolean(splt[3]);
            LastUpdate = DateTime.FromBinary(Convert.ToInt64(splt[4]));
            Count = Convert.ToInt32(splt[5]);
        }

        public Repo(string uri, string name, bool isFile) {
            name.Replace("§", string.Empty);
            uri.Replace("§", string.Empty);

            IsFile = isFile;
            URI = uri;
            if (GithubURL.IsHostedOnGit(URI) || URI.Contains("github.com"))
                GitURL = new GithubURL(URI);

            Name = name;
            if (!isFile) LastUpdate = GetLastModifedDate();
            else {
                if (File.Exists(uri)) 
                    LastUpdate = File.GetLastWriteTime(uri);
            }
        }

        public DateTime GetLastModifedDate() {
            if (GitURL is null || !GitURL.IsValid) return ParserUtils.GetDateTime(URI);
            else return GitURL.GetLastUpdateTime();
        }

        public HashSet<string> GetEnteries(bool setCount = false, bool setTime = false) {
            _hasUpdate = false;
            LogEx.LogRepoEvent("Parsing Enteries", $"Grabbing Enteries for >> {Name} >> {URI}");
            var data = ParserUtils.DownloadRepoText(URI);
            if (setCount) {
                LogEx.LogRepoEvent("Setting Count", $"Setting Text Length / Count of Data for the Repo >> {Name} >> {URI}");
                Count = data.Length;
            }

            if (setTime) {
                LogEx.LogRepoEvent("Setting Time", $"Setting Last Update Time for the Repo >> {Name} >> {URI}");
                LastUpdate = DateTime.Now;
            }

            return RepoCore.Allowed.Filter(ParserUtils.ParseFromTextEx(data, true));
        }

        public bool HasUpdatedTimeStamp() {
            if(!_hasUpdate && GetLastModifedDate() > LastUpdate) 
                _hasUpdate = true;

            return _hasUpdate;
        }

        public string ToStringEx()
            => $"\nName: {Name}\nURI: {URI}\nLastUpdate: {LastUpdate}\nCount: {Count}\n";
            
        public override string ToString() 
            => $"{Name}§{URI}§{IsFile}§{Enabled}§{LastUpdate.ToBinary()}§{Count}";
    }
}
