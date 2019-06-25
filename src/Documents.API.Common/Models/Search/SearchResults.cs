namespace Documents.API.Common.Models.Search
{
    using System.Collections.Generic;

    public class SearchResults : PagedResults<IndexedItem>
    {
        public string DebugQuery { get; set; }

        public IEnumerable<FacetModel> Facets { get; set; }
    }
}
