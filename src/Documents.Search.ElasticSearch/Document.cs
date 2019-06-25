namespace Documents.Search.ElasticSearch
{
    using Nest;
    using System;
    using System.Collections.Generic;

    [ElasticsearchType(IdProperty = nameof(UniqueKey))]
    public class Document
    {
        [Keyword]
        public string UniqueKey { get; set; }

        public DateTime Indexed { get; set; }

        [Keyword]
        public string OrganizationKey { get; set; }
        [Keyword]
        public string FolderKey { get; set; }
        [Keyword]
        public string FileKey { get; set; }

        [Keyword]
        public string Type { get; set; }


        [Text(Fielddata = true)]
        public string Name { get; set; }

        [Keyword]
        public string Extension { get; set; }
        [Keyword]
        public string MimeType { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }

        [Text(TermVector = TermVectorOption.WithPositionsOffsets)]
        public string Content { get; set; }

        [Text(Analyzer = "whitespace")]
        public string ACL_0 { get; set; }
        [Text(Analyzer = "whitespace")]
        public string ACL_1 { get; set; }
        [Text(Analyzer = "whitespace")]
        public string ACL_2 { get; set; }
        [Text(Analyzer = "whitespace")]
        public string ACL_3 { get; set; }
        [Text(Analyzer = "whitespace")]
        public string ACL_4 { get; set; }

        public IDictionary<string, string> Fields { get; set; }
        public IDictionary<string, string> Metadata { get; set; }

        [Nested]
        [PropertyName("facets")]
        public List<FacetString> FacetStrings { get; set; }
    }
}
