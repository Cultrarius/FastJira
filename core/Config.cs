using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Text;

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
            Configuration Config = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);

            SetString(Config, "JiraUser", JiraUser);
            SetString(Config, "JiraPassword", JiraPassword);
            SetString(Config, "JiraServer", JiraServer);

            Config.Save(ConfigurationSaveMode.Modified);
        }

        private string GetString(string Key)
        {
            return ConfigurationManager.AppSettings.Get(Key);
        }

        private void SetString(Configuration Config, string Key, string Value)
        {
            Config.AppSettings.Settings.Remove(Key);
            Config.AppSettings.Settings.Add(Key, Value);
        }
    }
}
