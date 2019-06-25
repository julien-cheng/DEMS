namespace Documents.Search
{
    using Documents.API.Common.Models.Search;
    using System.Collections.Generic;

    public interface ISearchResults
    {
        IEnumerable<ISearchResult> Rows { get; }
        long TotalMatches { get; }

        IEnumerable<FacetModel> Facets { get; }

        string DebugQuery { get; }
    }
}
