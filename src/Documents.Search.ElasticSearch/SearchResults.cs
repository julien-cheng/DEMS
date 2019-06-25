namespace Documents.Search.ElasticSearch
{
    using Documents.API.Common.Models.Search;
    using System.Collections.Generic;

    public class SearchResults : ISearchResults
    {
        public IEnumerable<ISearchResult> Rows { get; set; }
        public long TotalMatches { get; set; }

        public IEnumerable<FacetModel> Facets { get; set; }

        public string DebugQuery { get; set; }
    }
}
