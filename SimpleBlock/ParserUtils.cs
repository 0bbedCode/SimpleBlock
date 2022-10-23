using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleBlock {
    public class ParserUtils {
        static ParserUtils() {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.DefaultConnectionLimit = 9999;
        }

        public static readonly string Pattern_1 = "(?:(?:https?|ftp):\\/\\/)?[\\w/\\-?=%.]+\\.[\\w/\\-&?=%.]+";
        public static readonly string Pattern_2 = "(((((http|ftp|https|gopher|telnet|file|localhost):\\/\\/)|(www\\.)|(xn--)){1}([\\w_-]+(?:(?:\\.[\\w_-]+)+))([\\w.,@?^=%&:\\/~+#-]*[\\w@?^=%&\\/~+#-])?)|(([\\w_-]{2,200}(?:(?:\\.[\\w_-]+)*))((\\.[\\w_-]+\\/([\\w.,@?^=%&:\\/~+#-]*[\\w@?^=%&\\/~+#-])?)|(\\.((org|com|net|edu|gov|mil|int|arpa|biz|info|unknown|one|ninja|network|host|coop|tech)|(jp|br|it|cn|mx|ar|nl|pl|ru|tr|tw|za|be|uk|eg|es|fi|pt|th|nz|cz|hu|gr|dk|il|sg|uy|lt|ua|ie|ir|ve|kz|ec|rs|sk|py|bg|hk|eu|ee|md|is|my|lv|gt|pk|ni|by|ae|kr|su|vn|cy|am|ke))))))(?!(((ttp|tp|ttps):\\/\\/)|(ww\\.)|(n--)))";

        public static readonly List<string> Redirects = new List<string> { "0.0.0.0", "127.0.0.1" };
        public static readonly List<string> Bad = new List<string> { //For now we keep this 
            "localhost", 
            "localhost.localdomain", 
            "local", 
            "broadcasthost", 
            "ip6-localhost", 
            "ip6-loopback", 
            "ip6-localnet", 
            "ip6-mcastprefix", 
            "ip6-allnodes", 
            "ip6-allrouters" , 
            "ip6-allhosts", 
            "0.0.0.0" };

        public static List<string> ParseExternalHosts(string file) {
            if (!File.Exists(file))
                return null;

            try {
                var dic = new Dictionary<string, string>();
                var lns = File.ReadAllLines(file);
                foreach(var entry in lns) {
                    if (entry.StartsWith("#") || !Utils.StringIsValid(entry))
                        continue;

                    var ln = RemoveBegWhitespace(entry);
                }

            }
            catch(Exception e) { LogEx.LogError("ERROR Opening File", e); }
            return null;
        }

        public static List<Entry> ParseFromFile(string file) {
            if (!File.Exists(file))
                return null;

            try {
                return ParseFromTextEx(File.ReadAllText(file)).Select(e => new Entry(e)).ToList();
            }
            catch (Exception e) { LogEx.LogError("ERROR Opening File", e); }
            return null;
        }

        public static List<Entry> ParseFromURL(string URL) {
            if (!Utils.StringIsValid(URL))
                return null;

            try {
                using (WebClient cli = new WebClient()) {
                    return ParseFromTextEx(cli.DownloadString(URL)).Select(e => new Entry(e)).ToList();
                }
            }
            catch (Exception e) { LogEx.LogError("ERROR Downloading", e); }
            return null;
        }

        public static HashSet<string> ParseFromTextEx(string data/*, string comment = null*/) {
            var lst = new HashSet<string>();
            if (!Utils.StringIsValid(data)) {
                return lst;
            }

            LogEx.LogData("Parsing Data", "Parsing Text Data");

            data = data.ToLower();

            try {
                if (data.Contains("address=/") && data.Contains("/#"))
                    data = data.Replace("address=/", string.Empty).Replace("/#", string.Empty);

                var entries = data.Split('\n');
                foreach (var itm in entries) {
                    if (itm.StartsWith("#") || !Utils.StringIsValid(itm) || itm.StartsWith("!") || itm.StartsWith("["))
                        continue;

                    var eitm = itm;
                    string entry = null;
                    if (itm.StartsWith("||")) {//adblock plus config
                        entry = FindEntry(itm.Substring(2));
                    }
                    else {
                        entry = FindEntry(itm);
                        if (entry is null)
                            entry = RegExMatch(itm);
                        //if (entry is null)
                        //    entry = new Entry(RegExMatch(itm));//does not inclue comment
                    }

                    if (entry != null) {
                        //if (Utils.StringIsValid(comment))
                        //    entry.Comment = comment;

                        lst.Add(entry);
                    }
                }
            }
            catch (Exception e) { LogEx.LogError("Parsing Text Error", e); }
            LogEx.LogData("Parsed Data Count", lst.Count.ToString());
            return lst;
        }

        public static string FindOp(string data) {
            if (!Utils.StringIsValid(data))
                return null;

            if (data.StartsWith("www.")) return data.Substring(4);
            else return "www." + data;
        }

        public static string DownloadRepoText(string url) {
            try {
                using (WebClient cli = new WebClient())
                    return cli.DownloadString(url);
            }
            catch(Exception e) { LogEx.LogError("Failed to Download", e.Message + $"\n{url}"); return string.Empty; }
        }

        //https://learn.microsoft.com/en-us/dotnet/api/system.net.httpwebresponse.lastmodified?redirectedfrom=MSDN&view=net-6.0#System_Net_HttpWebResponse_LastModified
        public static DateTime GetDateTime(string url) {
            try {
                var uri = new Uri(url);
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                var last = myHttpWebResponse.LastModified;
                myHttpWebResponse.Close();
                return last;
            }
            catch(Exception e) { LogEx.LogError("Error Getting Modified Date", e); }
            return DateTime.Now;
        }

        public static Entry FindEntryEx(string data) {
            var enry = FindEntry(data);
            if (enry is null)
                return null;

            return new Entry(enry);
        }

        public static string FindEntry(string data) {
            if (!Utils.StringIsValid(data) || data.StartsWith(" ") || data.StartsWith("\t") || data.StartsWith("#"))
                return null;

            var strb = new StringBuilder();
            var iscm = false;
            var wait = false;
            for(var i = 0; i < data.Length; i++) {
                if (data[i] == '\n' || data[i] == '\0')
                    break;

                if (data[i] == '#') {
                    iscm = true;
                    strb.Append("§#");
                    if (wait) 
                        wait = false;

                    continue;
                }

                if (/*(data[i] == '#' && !iscm) ||*/ data[i] == '^' || data[i] == '|')
                    continue;

                if(i > 2 && (data[i] == '\t' || char.IsWhiteSpace(data[i]) && !iscm)) {
                    if (!wait)
                        wait = true;

                    continue;
                }

                if (wait) {
                    wait = false;
                    strb.Append("§");
                }

                strb.Append(data[i]);
            }

            var after = strb.ToString();
            if (after.Contains("§")) {
                var splt = after.Split('§');
                if (Redirects.Contains(splt[0]))
                    return splt[1];
                else
                    return splt[0];
            }

            return after;
        }

        public static bool IsBad(string data) {
            if (data.Contains("§")) {
                var splt = data.Split('§');
                if (Bad.Contains(splt[1])) 
                    return true;
            }

            return Bad.Contains(data);
        }

        public static string RemoveBegWhitespace(string data) {
            if (!Utils.StringIsValid(data) || (!data.StartsWith(" ") && !data.StartsWith("\t")))
                return data;

            for (var i = 0; i < data.Length; i++) {
                if (char.IsWhiteSpace(data[i]) || data[i] == '\t')
                    continue;

                return data.Substring(i);
            }

            return data;
        }

        public static string RegExMatch(string data) {
            if (!Utils.StringIsValid(data))
                return data;

            var mtch = Regex.Match(data, Pattern_1);
            if (!mtch.Success)
                mtch = Regex.Match(data, Pattern_2);

            if (!mtch.Success)
                return data;

            return mtch.Value;
        }

        public static string CleanGeneric(string data) {
            if (!Utils.StringIsValid(data))
                return data;

            if (data.Contains("\t") && !data.StartsWith("\t"))
                data = data.Split('\t')[0];

            if (data.StartsWith(" "))
                data = data.Substring(1);

            return CleanString(data).Replace(" ", string.Empty);
        }

        public static string CleanEnd(string data) {
            if (data.EndsWith("#"))
                return data.TrimEnd('#');
            if (data.EndsWith("^"))
                return data.TrimEnd('^');

            return data;
        }

        public static string CleanRedirect(string data) {
            if (data.StartsWith("0.0.0.0"))
                return data.Replace("0.0.0.0", string.Empty);
            if (data.StartsWith("127.0.0.1"))
                return data.Replace("127.0.0.1", string.Empty);
            return data;
        }

        public static string CleanString(string data) {
            if (!Utils.StringIsValid(data))
                return data;

            return data.Replace("\n", string.Empty).Replace("\t", string.Empty).Replace("\0", string.Empty);
        }

        public static string ParseDNSMasq(string data) {
            var c = data.Replace("address=/", string.Empty);
            if (c.EndsWith("#"))
                c = c.TrimEnd('#');

            return c;
        }
    }
}
