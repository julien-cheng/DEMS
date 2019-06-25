namespace Documents.Clients.Manager.Controllers
{
    using Common;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class SearchController : ManagerControllerBase
    {
        private readonly APIConnection Connection;
        private readonly SearchService Search;

        public SearchController(
            ILogger<SearchController> logger,
            APIConnection connection,
            SearchService search
        )
        {
            Logger = logger;
            Connection = connection;
            Search = search;
        }

        [HttpGet]
        public async Task<SearchResponse> Get(Models.Requests.SearchRequest searchRequest)
        {
            var organizationKey = searchRequest.Filters?.FirstOrDefault(f => f.Name == "organizationKey")?.Value;
            var folderKey = searchRequest.Filters?.FirstOrDefault(f => f.Name == "folderKey")?.Value;
            var pathKey = searchRequest.Filters?.FirstOrDefault(f => f.Name == "pathKey")?.Value;
            var organization = await Connection.Organization.GetAsync(
                new OrganizationIdentifier(organizationKey ?? Connection.UserIdentifier.OrganizationKey)
            );

            if (string.IsNullOrWhiteSpace(searchRequest.Keyword))
                searchRequest.Keyword = "*";

            searchRequest.Filters = searchRequest.Filters
                .Where(f => f.Name != "organizationKey")
                .Where(f => f.Name != "folderKey")
                .Where(f => f.Name != "pathKey")
                .ToList();

            if (pathKey != null)
            {
                var filters = searchRequest.Filters ?? new List<Models.Requests.SearchRequest.Filter>();

                filters.Add(new Models.Requests.SearchRequest.Filter
                {
                    Name = "_path",
                    Value = pathKey
                });

                searchRequest.Filters = filters;
            }

            searchRequest.FolderIdentifier = new FolderIdentifier
            {
                OrganizationKey = organizationKey,
                FolderKey = folderKey
            };

            return await Search.SearchAsync(organization, searchRequest);
        }
    }
}