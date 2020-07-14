using Fast_Jira.api;
using Fast_Jira.core;
using Fast_Jira.ui;
using Lifti;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fast_Jira
{
    public static class Constants
    {
        public const string DateTimeUiFormat = "dd.MM.yyyy HH:mm";
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DataVault Vault = new DataVault();
        private SearchWindow SearchWindow;
        private readonly Config AppConfig = new Config();
        private int UpdateTicker = 0;
        private bool SelectionActive = true;

        public MainWindow()
        {
            Vault.InitFromDisk();
            ConfigureClient();

            InitializeComponent();
            ConfigureUI();

            RefreshDisplayedIssue();
            UrlTextbox.Text = Vault.DisplayedIssue?.Key;

            Thread Watchdog = new Thread(WatchClipboard)
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            if (Watchdog.TrySetApartmentState(ApartmentState.STA))
            {
                Watchdog.Start();
            }
            else
            {
                logger.Error("Unable to set thread state for clipboard watcher");
            }

            logger.Debug("Application startup done.");
        }

        private void RefreshIssueHistory()
        {
            HistoryList.Items.Clear();
            int k = 1;
            foreach (Issue Item in Vault.GetAllIssuesSorted())
            {
                string Hotkey = k <= 9 ? "(" + k + ")" : "";
                k++;
                HistoryEntry Entry = new HistoryEntry(Hotkey, Item.Key, Item.Summary, Vault.GetWrappedImage(Item.Type.IconUrl));
                HistoryList.Items.Add(Entry);

                if (Item.Key == Vault.DisplayedIssue.Key)
                {
                    SelectionActive = false;
                    HistoryList.SelectedItem = Entry;
                    SelectionActive = true;
                }
            }
        }

        private void ConfigureUI()
        {
            Style = (Style)FindResource(typeof(Window));
            ProgressBar.IsIndeterminate = false;

            buttonSettings.Command = new RelayCommand(SettingsCommand_Executed);
            buttonBrowser.Command = new RelayCommand(BrowserCommand_Executed);

            UrlErrorText.Text = "";
            StatusText.Text = "";
            UrlTextbox.Focus();

            KeyDown += MainWindow_KeyDown;
            MouseWheel += MainWindow_MouseWheel;
            MarkdownViewer.MouseWheel += MainWindow_MouseWheel;
            HistoryList.SelectionChanged += HistoryList_SelectionChanged;
            UrlTextbox.KeyDown += UrlTextboxKeyDownHandler;
            DescriptionScrollView.ScrollChanged += DescriptionScrollView_ScrollChanged;
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0)
            {
                double CurrentOffset = DescriptionScrollView.VerticalOffset;
                double TargetScroll = Math.Clamp(CurrentOffset - e.Delta, 0, DescriptionScrollView.ScrollableHeight);
                DescriptionScrollView.ScrollToVerticalOffset(TargetScroll);
                e.Handled = true;
            }
        }

        private void DescriptionScrollView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Vault.DisplayedIssue != null)
            {
                Vault.DisplayedIssue.DescriptionScrollPosition = e.VerticalOffset;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && Clipboard.ContainsText())
            {
                string Content = Clipboard.GetText().Trim();
                UrlTextbox.Text = Content;
                UrlTextCommitted();
                e.Handled = true;
            }
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (SearchWindow == null)
                {
                    SearchWindow = new SearchWindow()
                    {
                        Vault = Vault,
                        Owner = this
                    };
                    SearchWindow.SearchResultSelected += SearchWindow_SearchResultSelected;
                }
                SearchWindow.Display();
                e.Handled = true;
            }

            for (int i = 0; i < 9; i++)
            {
                Key key = (Key)(i + (int)Key.D1);
                if (e.Key == key && HistoryList.Items.Count >= i + 1)
                {
                    HistoryList.SelectedIndex = i;
                }
            }

            if (e.Key == Key.F5 && Vault.DisplayedIssue?.Key != null)
            {
                IssueDisplayRequested(Vault.DisplayedIssue?.Key, true);
            }
        }

        private void SearchWindow_SearchResultSelected(string SelectedIssueKey)
        {
            if (Vault.HasIssueCached(SelectedIssueKey))
            {
                Vault.DisplayedIssue = Vault.GetCachedIssue(SelectedIssueKey);
                RefreshDisplayedIssue();
                Focus();
            }
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if (e.AddedItems.Count == 1 && SelectionActive)
            {
                HistoryEntry Selected = e.AddedItems[0] as HistoryEntry;
                if (Selected.IssueKey != Vault.DisplayedIssue?.Key)
                {
                    UrlTextbox.Text = Selected.IssueKey;
                    IssueDisplayRequested(Selected.IssueKey);
                }
            }
        }

        private void ConfigureClient()
        {
            ClientCredentials Credentials = Vault.JiraAPI.Credentials as ClientCredentials;
            Credentials.Username = AppConfig.JiraUser;
            Credentials.Password = AppConfig.JiraPassword;
            Vault.JiraAPI.BaseUri = new Uri(AppConfig.JiraServer);
        }

        private string FormatTime(DateTime? Input)
        {
            if (!Input.HasValue)
            {
                return "";
            }
            DateTime Time = Input.Value;
            string TimeString = Time.ToLocalTime().ToString(Constants.DateTimeUiFormat);
            double DaysAgo = DateTime.Now.Subtract(Time).TotalDays;
            string DaysAgoString = DaysAgo <= 1 ? "today" : Math.Floor(DaysAgo) + " days ago";
            return TimeString + " (" + DaysAgoString + ")";
        }

        private void RefreshComments()
        {
            List<Comment> Comments = Vault.DisplayedIssue?.Comments;
            if (Comments == null || Comments.Count == 0)
            {
                CommentPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                CommentPanel.Visibility = Visibility.Visible;
                CommentList.Items.Clear();
                foreach (Comment Comment in Comments)
                {
                    CommentDetails Details = new CommentDetails
                    {
                        AuthorName = Comment.Author?.DisplayName,
                        Body = Comment.Body,
                        Created = FormatTime(Comment.Created),
                        AuthorIcon = Vault.GetWrappedImage(Comment.Author?.AvatarUrl)
                    };
                    CommentList.Items.Add(Details);
                }
            }
        }

        public void RefreshDisplayedIssue()
        {
            Stopwatch Watch = Stopwatch.StartNew();

            DetailsStatus.Text = Vault.DisplayedIssue?.Status?.Name;
            DetailsStatus.Background = GetStatusColor(Vault.DisplayedIssue?.Status?.Color);
            DetailsPriority.Text = Vault.DisplayedIssue?.Priority?.Name;
            DetailsResolution.Text = Vault.DisplayedIssue?.Resolution;
            DetailsType.Text = Vault.DisplayedIssue?.Type?.Name;
            DetailsAssignee.Text = Vault.DisplayedIssue?.Assignee?.DisplayName;
            DetailsReporter.Text = Vault.DisplayedIssue?.Reporter?.DisplayName;
            DetailsCreated.Text = FormatTime(Vault.DisplayedIssue?.Created);
            DetailsUpdated.Text = FormatTime(Vault.DisplayedIssue?.Updated);

            UpdateImage(TypeImage, Vault.DisplayedIssue?.Type?.IconUrl);
            UpdateImage(AssigneeImage, Vault.DisplayedIssue?.Assignee?.AvatarUrl);
            UpdateImage(ReporterImage, Vault.DisplayedIssue?.Reporter?.AvatarUrl);

            RefreshComments();
            RefreshAttachments();

            SubtaskGroup.Visibility = Vault.DisplayedIssue?.Subtasks?.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            SummaryText.Text = Vault.DisplayedIssue?.Summary == null ? "- issue summary missing -" : Vault.DisplayedIssue.Summary;
            MarkdownViewer.Markdown = Vault.DisplayedIssue?.Description;
            DescriptionScrollView.ScrollToVerticalOffset(Vault.DisplayedIssue == null ? 0 : Vault.DisplayedIssue.DescriptionScrollPosition);

            RefreshIssueHistory();

            logger.Trace("UI refresh done in {0}ms", Watch.ElapsedMilliseconds);
        }

        private void RefreshAttachments()
        {
            List<Attachment> Attachments = Vault.DisplayedIssue?.Attachments;
            if (Attachments == null || Attachments.Count == 0)
            {
                AttachmentsGroup.Visibility = Visibility.Collapsed;
            }
            else
            {
                AttachmentsGroup.Visibility = Visibility.Visible;
                AttachmentList.Items.Clear();
                foreach (Attachment Attachment in Attachments)
                {
                    ImageSource Source;
                    if (!string.IsNullOrWhiteSpace(Attachment.Thumbnail) && Vault.HasImageCached(Attachment.Thumbnail))
                    {
                        Source = Vault.GetWrappedImage(Attachment.Thumbnail);
                    }
                    else
                    {
                        // look at that resource path, who thought that this was a good idea?
                        Source = new BitmapImage(new Uri("pack://application:,,,/Images/file.png"));
                    }
                    AttachmentDetails Details = new AttachmentDetails
                    {
                        AttachmentName = Attachment.FileName,
                        ID = Attachment.ID,
                        AttachmentThumbnail = Source
                    };
                    AttachmentList.Items.Add(Details);
                }
            }
        }

        private void UpdateImage(Image Element, string IconUrl)
        {
            if (Vault.HasImageCached(IconUrl))
            {
                Element.Source = Vault.GetWrappedImage(IconUrl);
                Element.Visibility = Visibility.Visible;
            }
            else
            {
                Element.Source = null;
                Element.Visibility = Visibility.Collapsed;
            }
        }

        private Brush GetStatusColor(string color)
        {
            if ("green".Equals(color))
            {
                return Brushes.PaleGreen;
            }
            if ("blue-gray".Equals(color))
            {
                return Brushes.LightBlue;
            }
            if ("yellow".Equals(color))
            {
                return Brushes.PaleGoldenrod;
            }
            return Brushes.Transparent;
        }

        private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show($"URL: {e.Parameter}");
        }

        private void ClickOnImage(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show($"Image: {e.Parameter}");
        }

        private void UrlTextboxKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(UrlTextbox.Text))
            {
                UrlTextCommitted();
            }
        }

        private void UrlTextCommitted()
        {
            UrlErrorText.Text = "";
            string IssueName = UrlTextbox.Text.Trim();
            if (IssueName.StartsWith("http"))
            {
                if (!IssueName.StartsWith(AppConfig.JiraServer))
                {
                    UrlErrorText.Text = "You can only see issues from your configured server (" + AppConfig.JiraServer + ").";
                    return;
                }
                string[] segments = new Uri(IssueName).Segments;
                if (segments.Length == 0)
                {
                    UrlErrorText.Text = "Whatever that is, it's not a valid url to a jira issue.";
                    return;
                }
                IssueName = segments[^1];
            }

            if (!Regex.IsMatch(IssueName, @"^[^-]+-\d+$"))
            {
                UrlErrorText.Text = "Mate, that does not look like the name of a jira issue.";
                return;
            }
            UrlTextbox.Text = IssueName;

            IssueDisplayRequested(IssueName);
        }

        private void IssueDisplayRequested(string IssueName, bool ForceUpdate = false)
        {
            if (Vault.HasIssueCached(IssueName))
            {
                // check if the issue has become stale (after 1 day) and refresh it if that is the case
                Issue CachedIssue = Vault.GetCachedIssue(IssueName);
                DateTime LastAccessed = CachedIssue.LastAccess;
                if (ForceUpdate || LastAccessed == null || DateTime.Now.Subtract(LastAccessed).TotalDays >= 1) //TODO: put amount in config
                {
                    LoadIssue(IssueName);
                }
                else
                {
                    Vault.DisplayedIssue = CachedIssue;
                    RefreshDisplayedIssue();
                }
            }
            else
            {
                LoadIssue(IssueName);
            }
        }

        private void LoadIssue(string issueName)
        {
            ProgressBar.IsIndeterminate = true;
            StatusText.Text = "Loading issue " + issueName + "...";

            UpdateTicker++;
            BackgroundWorker worker = new IssueLoader(UpdateTicker);
            worker.DoWork += LoadIssueWorker_DoWork;
            worker.RunWorkerCompleted += LoadIssueWorker_RunWorkerCompleted;
            worker.ProgressChanged += LoadIssueWorker_IntermediateResult;
            worker.RunWorkerAsync(issueName);
        }

        private class IssueLoader : BackgroundWorker
        {
            public readonly int WorkerTick;
            public Exception Error;

            public IssueLoader(int updateTicker)
            {
                WorkerTick = updateTicker;
                WorkerReportsProgress = true;
            }
        }

        void LoadIssueWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string issueID = e.Argument as string;
            if (string.IsNullOrWhiteSpace(issueID))
            {
                return;
            }

            try
            {
                if (Vault.HasIssueCached(issueID))
                {
                    Vault.DisplayedIssue = Vault.GetCachedIssue(issueID);
                    (sender as BackgroundWorker).ReportProgress(0);
                }
                e.Result = Vault.LoadIssue(issueID);
            }
            catch (Exception ex)
            {
                (sender as IssueLoader).Error = ex;
                logger.Error("Unable to load issue {0}: {1}", issueID, ex);
            }
        }

        void LoadIssueWorker_IntermediateResult(object sender, ProgressChangedEventArgs e)
        {
            StatusText.Text = "Checking for updates...";
            RefreshDisplayedIssue();
        }

        void LoadIssueWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((sender as IssueLoader).WorkerTick == UpdateTicker)
            {
                Exception Error = (sender as IssueLoader).Error;
                if (Error != null)
                {
                    UrlErrorText.Text = "Error! " + Error.GetType().Name + ": " + Error.Message;
                }

                ProgressBar.IsIndeterminate = false;
                StatusText.Text = "";

                Issue Result = e.Result as Issue;
                if (Result != null)
                {
                    Vault.DisplayedIssue = Result;
                    RefreshDisplayedIssue();
                }
            }
        }

        private void SettingsCommand_Executed(object parameter)
        {
            Settings SettingsWindow = new Settings
            {
                Owner = this,
                AppConfig = AppConfig
            };
            SettingsWindow.ShowDialog();
            ConfigureClient();
        }

        private void BrowserCommand_Executed(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Vault.DisplayedIssue?.Key) || !AppConfig.JiraServer.StartsWith("http"))
            {
                return;
            }
            string url = AppConfig.JiraServer + (AppConfig.JiraServer.EndsWith('/') ? "" : "/") + "browse/" + Vault.DisplayedIssue.Key;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void WatchClipboard()
        {
            while (true)
            {
                if (Clipboard.ContainsText())
                {
                    string Content = Clipboard.GetText().Trim();
                    try
                    {
                        string issueName = "";
                        if (Content.StartsWith(AppConfig.JiraServer))
                        {
                            string[] segments = new Uri(Content).Segments;
                            if (segments.Length > 0)
                            {
                                issueName = segments[^1];
                            }
                        }
                        else if (Regex.IsMatch(Content, @"^[^-]+-\d+$"))
                        {
                            issueName = Content;
                        }

                        Vault.PrefetchIssue(issueName);
                    }
                    catch (Exception e)
                    {
                        logger.Trace(e, "Clipboard exception");
                    }
                    Thread.Sleep(1000);
                }
            }
        }
    }

    public class DetailEntry
    {
        public string Title { get; set; }
        public string Value { get; set; }
    }

    public class HistoryEntry
    {
        public HistoryEntry(string hotkey, string key, string summary, ImageSource issueTypeImage)
        {
            Hotkey = hotkey;
            IssueKey = key;
            IssueSummary = summary;
            IssueTypeImage = issueTypeImage;
        }

        public string Hotkey { get; }
        public string IssueKey { get; }
        public string IssueSummary { get; }
        public ImageSource IssueTypeImage { get; }
    }

    public class CommentDetails
    {
        public string AuthorName { get; set; }
        public ImageSource AuthorIcon { get; set; }
        public string Created { get; set; }
        public string Body { get; set; }
    }

    public class AttachmentDetails
    {
        public string ID { get; set; }
        public string AttachmentName { get; set; }
        public ImageSource AttachmentThumbnail { get; set; }
    }
}
