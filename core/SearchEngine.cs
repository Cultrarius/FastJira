using Lifti;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FastJira.core
{
    public class SearchEngine
    {
        private readonly FullTextIndex<string> _searchIndex;

        public SearchEngine()
        {
            _searchIndex = new FullTextIndexBuilder<string>().Build();
        }

        public async ValueTask AddToIndex(string issueId, string text)
        {
            if (_searchIndex.IdLookup.Contains(issueId))
            {
                await _searchIndex.RemoveAsync(issueId);
            }
            await _searchIndex.AddAsync(issueId, text);
        }

        public List<SearchResult<string>> Search(string searchText)
        {
            string modifiedText = Regex.Replace(searchText.Trim(), @"(\S+)", "$1*");
            var result = new List<SearchResult<string>>(_searchIndex.Search(modifiedText));
            result.Sort((a, b) => a.FieldMatches.Count.CompareTo(b.FieldMatches.Count));
            return result;
        }
    }
}
