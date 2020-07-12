using Fast_Jira.core;
using System;
using System.Windows;

namespace Fast_Jira
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private Config appConfig;
        public Config AppConfig
        {
            get { return appConfig; }
            set {
                appConfig = value;
                jiraUrlInput.Text = value.JiraServer;
                jiraUserInput.Text = value.JiraUser;
                jiraPasswordInput.Password = value.JiraPassword;
            } 
        }

        public Settings()
        {
            InitializeComponent();

            Style = (Style)FindResource(typeof(Window));

            cancelButton.Command = new RelayCommand(CancelSettingsCommand_Executed);
            saveButton.Command = new RelayCommand(SaveSettingsCommand_CanExecute, SaveSettingsCommand_Executed);
        }

        private void CancelSettingsCommand_Executed(object parameter)
        {
            Close();
        }

        private bool SaveSettingsCommand_CanExecute(object parameter)
        {
            return !string.IsNullOrWhiteSpace(jiraUrlInput.Text) &&
                    !string.IsNullOrWhiteSpace(jiraUserInput.Text);
        }

        private void SaveSettingsCommand_Executed(object parameter)
        {
            AppConfig.JiraServer = jiraUrlInput.Text;
            AppConfig.JiraUser = jiraUserInput.Text;
            AppConfig.JiraPassword = jiraPasswordInput.Password;

            try
            {
                AppConfig.SaveToDisk();
                Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to save settings: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
