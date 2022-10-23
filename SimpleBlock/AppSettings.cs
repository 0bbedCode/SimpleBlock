using System;

namespace SimpleBlock {
    public class AppSettings {
        public static bool IsPaused {
            get => Get<bool>("Paused");
            set => Set("Paused", value);
        }

        public static void Set<T>(string name, T value) {
            try {
                Properties.Settings.Default[name] = value;
                Properties.Settings.Default.Save();
            }
            catch(Exception e) { LogEx.LogError($"Failed to Set App Setting >> {name}", e); }
        }

        public static T Get<T>(string name) {
            try {
                return (T)Properties.Settings.Default[name];
            }
            catch(Exception e) { LogEx.LogError($"Failed to Get App Setting >> {name}", e); }
            return default;
        }
    }
}
