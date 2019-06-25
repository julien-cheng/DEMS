namespace Documents.Search.ElasticSearch
{
    using System.Collections.Generic;

    public class SearchResposne
    {
        public int Took { get; set; }
        public SearchHits Hits { get; set; }
        public AggregationsRoot Aggregations { get; set; }

        public class SearchHits
        {
            public int Total { get; set; }
            public IEnumerable<DocumentWrapper> Hits { get; set; }
        }

        public class DocumentWrapper
        {
            public Document _Source { get; set; }
            public Dictionary<string, string[]> Highlight { get; set; }
        }

        public class AggregationsRoot
        {
            public FacetStringsSet Facet_Strings { get; set; }

        }

        public class FacetStringsSet
        {
            public long Doc_Count { get; set; }
            public FacetSet Facet_Name { get; set; }
        }

        public class FacetSet
        {
            public long Doc_Count_Error_Upper_Bound { get; set; }
            public long Sum_Other_Doc_Count { get; set; }
            public Bucket[] Buckets { get; set; }
        }

        public class Bucket
        {
            public string Key { get; set; }
            public long Doc_Count { get; set; }
            public FacetSet Facet_Value { get; set; }
        }
    }
}
