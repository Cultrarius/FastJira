using System.Configuration;
using System.Reflection;

namespace FastJira.core
{
    public class Config
    {
        public string JiraUser { get; set; }
        public string JiraPassword { get; set; }
        public string JiraServer { get; set; }
        public string ProxyServer { get; set; }
        public string ProxyUser { get; set; }
        public string ProxyPassword { get; set; }

        public Config()
        {
            RefreshFromDisk();
        }

        public void RefreshFromDisk()
        {
            JiraUser = GetString("JiraUser");
            JiraPassword = GetString("JiraPassword");
            JiraServer = GetString("JiraServer");
            ProxyServer = GetString("ProxyServer");
            ProxyUser = GetString("ProxyUser");
            ProxyPassword = GetString("ProxyPassword");
        }

        public void SaveToDisk()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);

            SetString(config, "JiraUser", JiraUser);
            SetString(config, "JiraPassword", JiraPassword);
            SetString(config, "JiraServer", JiraServer);
            SetString(config, "ProxyServer", ProxyServer);
            SetString(config, "ProxyUser", ProxyUser);
            SetString(config, "ProxyPassword", ProxyPassword);

            config.Save(ConfigurationSaveMode.Modified);
        }

        private static string GetString(string key)
        {
            return ConfigurationManager.AppSettings.Get(key);
        }

        private static void SetString(Configuration config, string key, string value)
        {
            config.AppSettings.Settings.Remove(key);
            config.AppSettings.Settings.Add(key, value);
        }
    }
}
