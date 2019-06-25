namespace Documents.Clients.Manager.Models.Responses
{
    using System.Collections.Generic;

    public class SearchResponse
    {
        public IEnumerable<ManagerFileSearchResult> Rows { get; set; }

        public long TotalMatches { get; set; }

        public IEnumerable<Facet> Facets {get; set;}

        public class Facet
        {
            public string Name { get; set; }
            public string Label { get; set; }
            public IEnumerable<FacetCount> Values { get; set; }
        }

        public class FacetCount
        {
            public string Value { get; set; }
            public string Label { get; set; }
            public long? Count { get; set; }
        }
    }
}
