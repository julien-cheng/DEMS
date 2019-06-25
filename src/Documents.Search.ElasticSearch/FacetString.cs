namespace Documents.Search.ElasticSearch
{
    using Nest;

    public class FacetString
    {
        [Keyword]
        public string Name { get; set; }

        [Keyword]
        public string Value { get; set; }
    }
}
