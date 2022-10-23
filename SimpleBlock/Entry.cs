using Microsoft.VisualBasic;
namespace SimpleBlock {
    public class Entry {
        public static readonly string CommentDelimiter = "§#";

        public string Redirect;
        public string Host;
        public string Comment;

        public Entry(string line) {
            if (!line.Contains("§")) {
                Host = line;
                Redirect = "0.0.0.0";
            }
            else {
                var splt = line.Split('§');
                Redirect = splt[0];
                Host = splt[1];

                if (line.Contains(CommentDelimiter))
                    Comment = Strings.Split(line, CommentDelimiter)[1];
            }
        }

        public Entry(string redirect, string host, string comment) {
            Redirect = redirect;
            Host = host;
            Comment = comment;
        }

        public bool IsBlocking()
            => Redirect == "0.0.0.0" || Redirect == "127.0.0.1";

        public override bool Equals(object obj) {
            if (obj is string)
                return ((string)obj).Equals(Host, System.StringComparison.OrdinalIgnoreCase);

            return false;
        }

        public override string ToString() 
            => Redirect + " " + Host + (Utils.StringIsValid(Comment) ? $"       #{Comment}" : string.Empty);
    }
}
