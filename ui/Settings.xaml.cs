using System;
using System.Windows;
using FastJira.core;

namespace FastJira.ui
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
                ProxyServerInput.Text = value.ProxyServer;
                ProxyUserInput.Text = value.ProxyUser;
                ProxyPasswordInput.Password = value.ProxyPassword;
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
            AppConfig.ProxyServer = ProxyServerInput.Text;
            AppConfig.ProxyUser = ProxyUserInput.Text;
            AppConfig.ProxyPassword = ProxyPasswordInput.Password;

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
