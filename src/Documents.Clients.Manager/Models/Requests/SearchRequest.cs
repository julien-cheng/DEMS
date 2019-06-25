namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class SearchRequest : ModelBase
    {
        public string Keyword { get; set; }
        public List<Filter> Filters { get; set; }
        public PagingArguments Paging { get; set; }

        // don't pass me up from the client. ignore me.
        public FolderIdentifier FolderIdentifier { get; set; }

        public class Filter
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}
