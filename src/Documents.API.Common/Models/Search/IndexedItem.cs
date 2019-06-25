namespace Documents.API.Common.Models.Search
{
    using System;
    using System.Collections.Generic;

    public class IndexedItem
    {
        public string ItemType { get; } = "File";
        public FileIdentifier FileIdentifier { get; set; }

        public string Name { get; set; }
        public string Extension { get; set; }
        public string MimeType { get; set; }

        public IDictionary<string, string> Metadata { get; set; }
        public IDictionary<string, string> Fields { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }

        public string[] Highlights { get; set; }
    }
}
