using System;

namespace SimpleBlock {
    public delegate void log_del(LogEx log);
    public enum LogType : uint {//we now move from Byte to uint to increase size
        Error, 
        Progress,
        Event,
        Info,
        IOFile,
        Repo,
        RepoConfig,
        Host,
        Update
    }

    public class LogEx : IDisposable {
        public static event log_del OnLog;

        public LogType TypeLog;
        public string LongMessage;
        public string ShortMessage;
        public Exception Exc;
        public DateTime TimeStamp;

        public LogEx(string msg, Exception e, LogType typeLog) {
            ShortMessage = e is null ? msg : e.GetType().Name;
            LongMessage = msg;
            Exc = e;
            TypeLog = typeLog;
            TimeStamp = DateTime.Now;
        }
        public string GetTypeLogName()
            => Enum.GetName(typeof(LogType), TypeLog);

        public static void LogError(string shrtmsg, string msg) {
            var lg = new LogEx(msg, null, LogType.Error) { ShortMessage = shrtmsg };
            OnLog?.Invoke(lg);
        }

        public static void LogError(string msg, Exception e = null)
            => OnLog?.Invoke(new LogEx(msg, e, LogType.Error));

        public static void LogData(string msg, LogType lgtype = LogType.Progress)
            => OnLog?.Invoke(new LogEx(msg, null, lgtype));

        public static void LogData(string shrtmsg, string msg, LogType lgtype = LogType.Progress) {
            var lg = new LogEx(msg, null, lgtype) { ShortMessage = shrtmsg };
            OnLog?.Invoke(lg);
        }

        public static void LogIsNULL(string itemName)
            => LogData($"{itemName} NULL", $"Object <{itemName}> is NULL or Empty. Invalid Object", LogType.Error);

        public static void LogIsNULL(string varName, string extraMessage)
            => LogData($"{varName} NULL", $"{varName} is NULL or Empty.\n" + extraMessage);

        public static void LogHost(string msg)
            => LogHost(msg, string.Empty);

        public static void LogHost(string msg, string data)
            => LogData(msg, data, LogType.Host);

        public static void LogRepoConfig(string msg, string data)
            => LogData(msg, data, LogType.RepoConfig);

        public static void LogRepoEvent(string msg, string repoOrData)
            => LogData(msg, repoOrData, LogType.Repo);

        public static void LogIOEvent(string msg, string fileOrData)
            => LogData(msg, fileOrData, LogType.IOFile);

        public static void LogNeedsUpdate()
            => LogNeedsUpdate(string.Empty);

        public static void LogNeedsUpdate(string dta)
            => LogData("RepoCore Update Available", $"RepoCore Has an Repo that Needs to be Updated to The Host File >> {dta}",  LogType.Event);

        public static void LogNoUpdatesFound()
            => LogNoUpdatesFound(string.Empty);

        public static void LogNoUpdatesFound(string dta)
            => LogData("RepoCore No Updates", $"RepoCore has no Available Updates to be written with the Host File >> {dta}");

        public static void LogIOException(string str, Exception e)
            => LogIOException(str + $"\n{e.Message}");

        public static void LogIOException(string str)
            => LogError("IOException", str);

        public static void LogIOExcetion(Exception e)
            => LogError("IOException", e);

        public static void LogCoreIsBusy()
            => LogError("Core Is Busy", "Core Is busy Either Updating/ Checking For Updates and or is Paused");

        public static void LogCoreIsBusy(string var)
            => LogError("Core Is Busy", "Core Is busy Either Updating/ Checking For Updates and or is Paused\n" + var);

        public override string ToString() {
            var lmsg = LongMessage == ShortMessage ? string.Empty : LongMessage;
            var emsg = Exc is null ? string.Empty : Exc.Message;
            return $"{GetTypeLogName()} >> {TimeStamp}\n\n{ShortMessage}\n{lmsg}\n\n{emsg}";
        }

        public void Dispose() {
            LongMessage = null;
            ShortMessage = null;
            Exc = null;
            GC.SuppressFinalize(this);
            //GC.Collect();
        }
    }
}
