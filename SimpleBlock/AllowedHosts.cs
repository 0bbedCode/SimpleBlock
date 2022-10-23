using System;
using System.IO;
using System.Collections.Generic;

namespace SimpleBlock {
    public class AllowedHosts : SimpleIO {
        public List<string> Allowed = new List<string>();
        public AllowedHosts(string path) : base(path) {
            RepoCore.Allowed = this;
            if (!File.Exists(Path)) File.Create(Path);
            else Read();
        }

        public List<Entry> FilterEx(List<Entry> itms) {
            try {
                LogEx.LogData("Filtering Data", "Removing Allowed Enteries from Data");
                for (var iLine = itms.Count; iLine-- > 0;) {
                    if (Allowed.Contains(itms[iLine].Host)) 
                        itms.RemoveAt(iLine);
                }
            }
            catch { }
            return itms;
        }

        public HashSet<string> Filter(HashSet<string> itms) {
            try {
                LogEx.LogData("Filtering Data", "Removing Allowed Enteries from Data");
                for(var i = 0; i < Allowed.Count; i++) {
                    var itm = Allowed[i];
                    if (itms.Contains(itm))
                        itms.Remove(itm);
                }
            }
            catch { }
            return itms;
        }

        public void Read() {
            try {
                Close();
                Allowed.Clear();
                Allowed.AddRange(File.ReadAllLines(Path));
            }
            catch(Exception e) { LogEx.LogError("Read Allowed Failed", e); }
        }

        public void WriteAllowed() {
            try {
                Clear();
                File.WriteAllLines(Path, Allowed);
            }
            catch(Exception e) { LogEx.LogError("Write Allowed Failed", e); }
        }
    }
}
