using System;
using System.IO;

namespace SimpleBlock {
    public abstract class SimpleIO {
        public readonly string Path;
        internal StreamWriter _writer = null;

        public SimpleIO(string path, bool create = true) {
            if (!File.Exists(path) && create)
                File.Create(path);
            
            Path = path;
        }

        public bool Clear() {
            try {
                lock (this) {
                    LogEx.LogIOEvent("Clearing File",  $"Clearing out all Data in File >> {Path}");
                    Close();
                    File.WriteAllText(Path, string.Empty);
                    return true;
                }
            }
            catch(Exception e) { LogEx.LogError($"Clearing File Failed", e.Message + $"\n\n{Path}"); return false; }//can assume we have no access
        }

        public bool Open() {
            try {
                LogEx.LogIOEvent("Opening File", $"Opening File Access >> {Path}");
                Close();
                _writer = new StreamWriter(Path, true);
                return true;
            }
            catch(Exception e) { LogEx.LogError($"Opening {Path} File Failed", e); return false; }
        }

        public void Close() {
            if (_writer is null)
                return;

            LogEx.LogIOEvent("Closing File", $"Closing File Access >> {Path}");
            _writer.Close();
            _writer = null;
        }

        public string[] TryReadAllLines() {
            lock (this) {
                try {
                    return File.ReadAllLines(Path);
                }
                catch (Exception e) { LogEx.LogError("ReadAllLines Error", e); return null; }
            }
        }
    }
}
