using Fast_Jira.core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Fast_Jira.ui
{
    public delegate void SearchResultSelectedEventHandler(string SelectedIssueKey);

    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        public event SearchResultSelectedEventHandler SearchResultSelected;
        public DataVault Vault { get; set; }

        private int selectionIndex;
        private bool changingResults;

        public SearchWindow()
        {
            InitializeComponent();

            KeyUp += SearchText_KeyUp;
            GotFocus += SearchWindow_GotFocus;
            SearchText.KeyUp += SearchText_KeyUp;
            ResultList.SelectionChanged += ResultList_SelectionChanged;
        }

        private void SearchWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source != ResultList)
            {
                SearchText.Focus();
            }
        }

        private void ResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultList.SelectedIndex >= 0) {
                selectionIndex = ResultList.SelectedIndex;
            }
            if (!changingResults)
            {
                ResultSelected();
            }
        }

        private void ResultSelected()
        {
            if (ResultList.Items.Count > 0)
            {
                ResultEntry entry = ResultList.SelectedItem as ResultEntry;
                if (entry != null)
                {
                    SearchResultSelected?.Invoke(entry.IssueKey);
                    Hide();
                }
            }
        }

        public void Display()
        {
            Show();
            Top = (Owner.Top + Owner.Height / 2) - Height / 2;
            Left = (Owner.Left + Owner.Width / 2) - Width / 2;

            SearchText.Focus();
            SearchText.SelectAll();
        }

        private void SearchText_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            changingResults = true;
            if (e.Key == Key.Escape)
            {
                Hide();
            }
            else if (e.Key == Key.Enter)
            {
                ResultSelected();
            }
            else if (e.Key == Key.Down)
            {
                selectionIndex++;
                ResultList.SelectedIndex = Math.Min(ResultList.Items.Count - 1, selectionIndex);
            }
            else if (e.Key == Key.Up)
            {
                selectionIndex--;
                ResultList.SelectedIndex = Math.Min(ResultList.Items.Count - 1, selectionIndex);
            }
            else
            {
                // update results
                string Text = SearchText.Text;
                var resultIssues = Vault.SearchIssues(Text);
                ResultList.Items.Clear();
                if (resultIssues.Count == 0)
                {
                    ResultList.Visibility = Visibility.Collapsed;
                    ResultEmptyText.Visibility = Visibility.Visible;
                }
                else
                {
                    ResultList.Visibility = Visibility.Visible;
                    ResultEmptyText.Visibility = Visibility.Collapsed;

                    for (int i = 0; i < resultIssues.Count && i < 12; i++)
                    {
                        Issue issue = resultIssues[i];
                        string assigneeText = "(" + issue.Assignee?.DisplayName + " / " + issue.Reporter?.DisplayName + ")";
                        ResultEntry Entry = new ResultEntry(issue.Key, issue.Summary, assigneeText, Vault.GetWrappedImage(issue.Type?.IconUrl));
                        ResultList.Items.Add(Entry);
                    }
                    ResultList.SelectedIndex = Math.Min(resultIssues.Count - 1, selectionIndex);
                }
            }
            changingResults = false;
        }
    }

    public class ResultEntry
    {
        public ResultEntry(string key, string summary, string assignee, ImageSource issueTypeImage)
        {
            IssueKey = key;
            IssueSummary = summary;
            IssueTypeImage = issueTypeImage;
            Assignee = assignee;
        }

        public string IssueKey { get; }
        public string Assignee { get; }
        public string IssueSummary { get; }
        public ImageSource IssueTypeImage { get; }
    }
}
