namespace Documents.API.Client
{
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.Search;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class SearchMethods
    {
        private readonly Connection Connection;
        public SearchMethods(Connection connection)
        {
            this.Connection = connection;
        }

        public async Task<SearchResults> SearchAsync(SearchRequest searchRequest)
        {
            return await Connection.APICallAsync<SearchResults>(HttpMethod.Get, APIEndpoint.Search,
                queryStringContent: new
                {
                    searchRequest
                }
            );
        }
    }
}
