using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

namespace SimpleBlock {
    public class GithubURL {
        //https://github.com/AdAway/AdAway/blob/40d1cbb123a499e8688b10ddc4d3234a8e952427/app/src/main/java/org/adaway/model/git/GitHostsSource.java#L33
        private static readonly string GITHUB_REPO_URL = "https://raw.githubusercontent.com/";
        private static readonly string GITHUB_GIST_URL = "https://gist.githubusercontent.com";
        private static readonly string GITLAB_URL = "https://gitlab.com/";

        public Uri URL;
        public string Owner;
        public string Repo;
        public string Blob;

        public bool IsValid = true;

        public GithubURL(string url) {
            if (!Utils.StringIsValid(url))
                return;

            URL = new Uri(url);
            var pth = URL.PathAndQuery;
            var pts = pth.Split('/');
            if (pts.Length < 5) {
                LogEx.LogError("Invalid String", $"String is Invalid to Parse GithubURL >> {url}");
                IsValid = false;
                return;
            }

            Owner = pts[1];
            Repo = pts[2];

            if (!IsHostedOnGit(url)) {
                if (new Regex("/").Matches(url).Count < 5 || !url.Contains("raw/master")) {
                    LogEx.LogError("Invalid String", "Github URL Is Invalid Format :/");
                    IsValid = false;
                    return;
                }

                LogEx.LogData("Parsing URL Blob", $"Parsing Github URL Not a Valid Guthub Format >> {url}");

                var blob = new StringBuilder();
                var aftrMaster = false;
                for (var i = 1; i < pts.Length; i++) {
                    if (pts[i] == "master" && pts[i - 1] == "raw") {
                        aftrMaster = true;
                        continue;
                    }

                    if (aftrMaster) {
                        if (i == pts.Length - 1) blob.AppendLine(pts[i]);
                        else blob.Append(pts[i] + "/");
                    }
                }

                Blob = blob.ToString();
            }
            else 
                Blob = string.Join("/", pts.Skip(4).ToArray());
        }

        public static bool IsHostedOnGit(string url)
            => url.StartsWith(GITLAB_URL) || url.StartsWith(GITHUB_GIST_URL) || url.StartsWith(GITHUB_REPO_URL);

        public DateTime GetLastUpdateTime() {
            try {
                var comitApiUrl = "https://api.github.com/repos/" + Owner + "/" + Repo +
                    "/commits?per_page=1&path=" + Blob;

                var str = string.Empty;
                using (WebClient cli = new WebClient()) {
                    cli.Headers.Add("user-agent", " Mozilla/5.0 (Windows NT 6.1; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0");
                    str = cli.DownloadString(comitApiUrl);
                }

                dynamic commits = JArray.Parse(str);
                DateTime dtme = commits[0].commit.committer.date;
                return dtme;
            }
            catch(Exception e) { LogEx.LogError("Failed to Grab DateTime", e); return DateTime.Now; }
        }
    }
}
