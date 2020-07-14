using System.Configuration;
using System.Reflection;

namespace Fast_Jira.core
{
    public class Config
    {
        public string JiraUser { get; set; }
        public string JiraPassword { get; set; }
        public string JiraServer { get; set; }

        public Config()
        {
            RefreshFromDisk();
        }

        public void RefreshFromDisk()
        {
            JiraUser = GetString("JiraUser");
            JiraPassword = GetString("JiraPassword");
            JiraServer = GetString("JiraServer");
        }

        public void SaveToDisk()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);

            SetString(config, "JiraUser", JiraUser);
            SetString(config, "JiraPassword", JiraPassword);
            SetString(config, "JiraServer", JiraServer);

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
