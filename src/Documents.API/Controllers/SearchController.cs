namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.Search;
    using Documents.API.Events;
    using Documents.Search;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;

    public class SearchController : APIControllerBase
    {
        private readonly ISearch Search = null;
        private readonly ISecurityContext SecurityContext;
        private readonly IEventSender EventSender;

        public SearchController(ISearch search, ISecurityContext securityContext, IEventSender eventSender)
             : base(securityContext)
        {
            this.Search = search;
            this.SecurityContext = securityContext;
            this.EventSender = eventSender;
        }

        [HttpGet]
        public async Task<SearchResults> FileSearch(SearchRequest searchRequest)
        {
            searchRequest.Paging = searchRequest.Paging ?? new PagingArguments
            {
                PageSize = 100,
                PageIndex = 0
            };

            var results = await this.Search.Search(
                SecurityContext.SecurityIdentifiers,
                searchRequest
            );

            await EventSender.SendAsync(new SearchEvent
            {
                DebugQuery = results.DebugQuery,
                SearchRequest = searchRequest
            });

            return new SearchResults
            {
                Rows = results.Rows
                    .Select(r => new IndexedItem
                    {
                        FileIdentifier = r.FileIdentifier,

                        Name = r.Name,
                        MimeType = r.MimeType,
                        Extension = r.Extension,

                        Metadata = r.Metadata,
                        Fields = r.Fields,

                        Created = r.Created,
                        Modified = r.Modified,

                        Highlights = r.Highlights
                    }),
                TotalMatches = results.TotalMatches,
                Facets = results.Facets,
                DebugQuery = results.DebugQuery
            };
        }
    }
}
