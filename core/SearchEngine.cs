using Lifti;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fast_Jira.core
{
    class SearchEngine
    {
        private readonly FullTextIndex<string> SearchIndex;

        public SearchEngine()
        {
            SearchIndex = new FullTextIndexBuilder<string>().Build();
        }

        public async ValueTask AddToIndex(string IssueID, string Text)
        {
            if (SearchIndex.IdLookup.Contains(IssueID))
            {
                await SearchIndex.RemoveAsync(IssueID);
            }
            await SearchIndex.AddAsync(IssueID, Text);
        }

        public List<SearchResult<string>> Search(string SearchText)
        {
            var Result = new List<SearchResult<string>>(SearchIndex.Search(SearchText));
            Result.Sort((A, B) => A.FieldMatches.Count.CompareTo(B.FieldMatches.Count));
            return Result;
        }
    }
}
