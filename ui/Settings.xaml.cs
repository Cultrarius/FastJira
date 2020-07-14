using System;
using System.Windows;
using Fast_Jira.core;

namespace Fast_Jira.ui
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings
    {
        private Config _appConfig;
        public Config AppConfig
        {
            get => _appConfig;
            set {
                _appConfig = value;
                JiraUrlInput.Text = value.JiraServer;
                JiraUserInput.Text = value.JiraUser;
                JiraPasswordInput.Password = value.JiraPassword;
            } 
        }

        public Settings()
        {
            InitializeComponent();

            Style = (Style)FindResource(typeof(Window));

            CancelButton.Command = new RelayCommand(CancelSettingsCommand_Executed);
            SaveButton.Command = new RelayCommand(SaveSettingsCommand_CanExecute, SaveSettingsCommand_Executed);
        }

        private void CancelSettingsCommand_Executed(object parameter)
        {
            Close();
        }

        private bool SaveSettingsCommand_CanExecute(object parameter)
        {
            return !string.IsNullOrWhiteSpace(JiraUrlInput.Text) &&
                    !string.IsNullOrWhiteSpace(JiraUserInput.Text);
        }

        private void SaveSettingsCommand_Executed(object parameter)
        {
            AppConfig.JiraServer = JiraUrlInput.Text;
            AppConfig.JiraUser = JiraUserInput.Text;
            AppConfig.JiraPassword = JiraPasswordInput.Password;

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
