namespace Documents.API.Common.Models
{
    using Documents.API.Common.Models.Search;
    using System.Collections.Generic;

    public class SearchRequest
    {
        public OrganizationIdentifier OrganizationIdentifier { get; set; }
        public FolderIdentifier FolderIdentifier { get; set; }
        public string KeywordQuery { get; set; }
        public string NativeQuery { get; set; }
        public IEnumerable<Filter> Filters { get; set; }
        public PagingArguments Paging { get; set; }
    }
}
