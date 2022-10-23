using System;

using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace SimpleBlock {
    internal class Utils {
        internal static readonly string HostPath = "C:\\Windows\\System32\\drivers\\etc\\hosts";
        internal static readonly string CurrentPath = AppDomain.CurrentDomain.BaseDirectory;
        internal static readonly string RepoConfig = CurrentPath + "\\Repos.txt";
        internal static readonly string AllowedConfig = CurrentPath + "\\Allowed.txt";


        internal static void SetDNSCacheState(bool enabled) {
            var val = enabled ? 2 : 4;
            Interaction.Shell($"reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache /t REG_DWORD /v Start /d {val} /f");
            LogEx.LogData("Restart PC", "Restart PC To Apply DNS Cache Changes!", LogType.Update);
        }

        internal static string FindHosts() {
            return HostPath;//add more
        }

        internal static bool StringIsValid(string str)
            => !string.IsNullOrEmpty(str) && !IsNullOrWhiteSpace(str);

        internal static bool IsDNSCacheEnabled() {
            using(var bk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)) {
                return (int)bk.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Dnscache").GetValue("Start") == 2;
            }
        }

        internal static bool IsNullOrWhiteSpace(string value) {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++) {
                if (!char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }
    }
}
