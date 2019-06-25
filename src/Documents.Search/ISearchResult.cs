namespace Documents.Search
{
    using Documents.API.Common.Models;
    using System;
    using System.Collections.Generic;

    public interface ISearchResult
    {
        FileIdentifier FileIdentifier { get; }

        string Name { get; }
        string Extension { get; }
        string MimeType { get; }

        DateTime? Created { get; }
        DateTime? Modified { get; }

        IDictionary<string, string> Metadata { get; set; }
        IDictionary<string, string> Fields { get; set; }

        string[] Highlights { get; }
    }
}
