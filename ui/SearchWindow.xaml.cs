using Fast_Jira.core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Fast_Jira.ui
{
    public delegate void SearchResultSelectedEventHandler(string selectedIssueKey);

    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow
    {
        public event SearchResultSelectedEventHandler SearchResultSelected;
        public DataVault Vault { get; set; }

        private int _selectionIndex;
        private bool _changingResults;

        public SearchWindow()
        {
            InitializeComponent();

            KeyUp += SearchText_KeyUp;
            GotFocus += SearchWindow_GotFocus;
            SearchText.KeyUp += SearchText_KeyUp;
            ResultList.SelectionChanged += ResultList_SelectionChanged;
            ResultList.MouseLeftButtonUp += ResultList_MouseLeftButtonUp;
        }

        private void ResultList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender != ResultList) return;
            e.Handled = true;
            ResultSelected();
        }

        private void SearchText_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            _changingResults = true;
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    break;
                case Key.Enter:
                    ResultSelected();
                    break;
                case Key.Down:
                    _selectionIndex++;
                    ResultList.SelectedIndex = Math.Min(ResultList.Items.Count - 1, _selectionIndex);
                    break;
                case Key.Up:
                    _selectionIndex--;
                    ResultList.SelectedIndex = Math.Min(ResultList.Items.Count - 1, _selectionIndex);
                    break;
                default:
                {
                    // update results
                    string text = SearchText.Text;
                    var resultIssues = Vault.SearchIssues(text);

                    if (resultIssues.Count == 0)
                    {
                        if (ResultList.Items.Count == 0)
                        {
                            ResultList.Visibility = Visibility.Collapsed;
                            ResultEmptyText.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        ResultList.Items.Clear();
                        ResultList.Visibility = Visibility.Visible;
                        ResultEmptyText.Visibility = Visibility.Collapsed;

                        for (int i = 0; i < resultIssues.Count && i < 12; i++)
                        {
                            Issue issue = resultIssues[i];
                            string assigneeText = "(" + (issue.Assignee?.DisplayName ?? "Unassigned") + ")";
                            ResultEntry entry = new ResultEntry(issue.Key, issue.Summary, assigneeText, Vault.GetWrappedImage(issue.Type?.IconUrl));
                            ResultList.Items.Add(entry);
                        }
                        ResultList.SelectedIndex = Math.Min(resultIssues.Count - 1, _selectionIndex);
                    }
                    break;
                }
            }
            _changingResults = false;
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
                _selectionIndex = ResultList.SelectedIndex;
            }
            if (!_changingResults)
            {
                ResultSelected();
            }
        }

        private void ResultSelected()
        {
            if (ResultList.Items.Count > 0 && ResultList.SelectedItem is ResultEntry entry)
            {
                SearchResultSelected?.Invoke(entry.IssueKey);
                Hide();
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
