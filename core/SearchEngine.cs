using Lifti;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FastJira.core
{
    public class SearchEngine
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly FullTextIndex<string> _searchIndex;

        public SearchEngine()
        {
            _searchIndex = new FullTextIndexBuilder<string>().Build();
        }

        public async ValueTask AddToIndex(Issue issue)
        {
            string issueId = issue.Key;
            if (_searchIndex.IdLookup.Contains(issueId))
            {
                await _searchIndex.RemoveAsync(issueId);
            }
            await _searchIndex.AddAsync(issueId, issue.ToFulltextDocument());
        }

        public async ValueTask AddToIndex(List<Issue> issues)
        {
            Stopwatch sw = Stopwatch.StartNew();
            _searchIndex.BeginBatchChange();
            int i = 0;
            foreach (var issue in issues)
            {
                await AddToIndex(issue);
                i++;
                if (i % 500 == 0)
                {
                    Logger.Debug("Indexed {0}/{1} issues...", i, issues.Count);
                }
            }

            await _searchIndex.CommitBatchChangeAsync();
            Logger.Debug("Indexed {0} issues in {1}ms", i, sw.ElapsedMilliseconds);
        }

        public List<SearchResult<string>> Search(string searchText)
        {
            string modifiedText = Regex.Replace(searchText.Trim(), @"(\S+)", "$1*");
            var result = new List<SearchResult<string>>(_searchIndex.Search(modifiedText));
            result.Sort((a, b) => a.FieldMatches.Count.CompareTo(b.FieldMatches.Count));
            return result;
        }

        public void Serialize(string filepath)
        {
            string json = JsonSerializer.Serialize<FullTextIndex<string>>(_searchIndex);
            File.WriteAllText(filepath, json);
        }
    }
}
