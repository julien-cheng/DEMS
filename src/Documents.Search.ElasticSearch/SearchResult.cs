namespace Documents.Search.ElasticSearch
{
    using Documents.API.Common.Models;
    using System;
    using System.Collections.Generic;

    public class SearchResult : ISearchResult
    {
        public FileIdentifier FileIdentifier { get; set; }

        public string Name { get; set; }
        public string Extension { get; set; }
        public string MimeType { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }

        public IDictionary<string, string> Metadata { get; set; }

        public string[] Highlights { get; set; }

        public IDictionary<string, string> Fields { get; set; }
    }
}
