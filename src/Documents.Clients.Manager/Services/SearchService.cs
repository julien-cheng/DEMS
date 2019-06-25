namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Models.Search;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class SearchService
    {
        protected readonly APIConnection Connection;

        public SearchService(APIConnection connection)
        {
            Connection = connection;
        }

        public async Task<SearchResponse> SearchAsync(OrganizationModel organization, Manager.Models.Requests.SearchRequest request)
        {
            var searchConfiguration = organization.Read<SearchConfiguration>("searchConfiguration");

            if (request.Filters?.Any() ?? false)
                request.Filters = request.Filters.Select(f =>
                {
                    if (f.Name == "pathKey")
                        f.Name = "_path";

                    return f;
                }).ToList();

            var results = await Connection.Search.SearchAsync(
                searchRequest: new SearchRequest
                {
                    OrganizationIdentifier = organization.Identifier,
                    FolderIdentifier = request.FolderIdentifier,
                    KeywordQuery = request.Keyword,
                    Filters = request.Filters?.Select(ff => new API.Common.Models.Search.Filter
                    {
                        Name = ff.Name,
                        Value = ff.Value
                    }),
                    Paging = request.Paging ?? new PagingArguments
                    {
                        PageIndex = 0,
                        PageSize = 500
                    }
                }
            );


            var response = new SearchResponse
            {
                Rows = results.Rows.Select(f => {

                    var alternativeViews = f.Metadata?.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);
                    var pathIdentifier = f.Metadata?.MetaPathIdentifierRead(f.FileIdentifier as FolderIdentifier, dontThrow: true);

                    var result = new ManagerFileSearchResult
                    {
                        Identifier = f.FileIdentifier,
                        PathIdentifier = pathIdentifier,

                        FullPath = pathIdentifier?.FullName,

                        Name = f.Name,
                        Views = ViewSetService.DetectFileViews(
                            organization,
                            f.FileIdentifier,
                            f.Metadata.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS),
                            f.Extension,
                            f.MimeType
                        ),
                        Created = f.Created,
                        Modified = f.Modified,

                        PreviewImageIdentifier = alternativeViews?.FirstOrDefault(v => v.SizeType == "Thumbnail")?.FileIdentifier,

                        Attributes = BuildAttributes(searchConfiguration, f),

                        Highlights = f.Highlights
                    };

                    result.ViewerType = result.Views.FirstOrDefault()?.ViewerType ?? Manager.Models.ManagerFileView.ViewerTypeEnum.Unknown;
                    result.Icons = result.Views.FirstOrDefault()?.Icons;

                    return result;
                }).ToList(), // if we don't ToList(), then deferred execution happens during the streaming, and you don't
                             // want to debug that again, trust me.

                TotalMatches = results.TotalMatches,

                Facets = DecorateFacets(searchConfiguration, results.Facets)
                
            };

            return response;
        }

        public Dictionary<string, object> BuildAttributes(
            SearchConfiguration searchConfiguration,
            IndexedItem row)
        {
            var fields = searchConfiguration?.DisplayFields;

            if (fields?.Any() ?? false)
            {
                return fields.ToDictionary(f => Label(f, searchConfiguration, f), f =>
                {
                    var value = row.Fields.ContainsKey(f)
                        ? row.Fields[f]
                        : null;

                    return Label($"{f}.{value}", searchConfiguration, value) as object;
                }).Where(f => f.Value != null).ToDictionary(f => f.Key, f => f.Value);
            }

            return null;
        }

        private string Label(string field, SearchConfiguration searchConfiguration, string defaultValue)
        {
            return searchConfiguration?.LanguageMap != null && (searchConfiguration?.LanguageMap.ContainsKey(field) ?? false)
                ? searchConfiguration?.LanguageMap[field]
                : defaultValue;
        }


        public IEnumerable<SearchResponse.Facet> DecorateFacets(
            SearchConfiguration searchConfiguration, 
            IEnumerable<API.Common.Models.Search.FacetModel> facets)
        {
            var languageMap = searchConfiguration?.LanguageMap;


            return
                facets?.Select(f => new SearchResponse.Facet
                {
                    Label = Label(f.Name, searchConfiguration, f.Name),
                    Name = f.Name,
                    Values = f.Values.Select(v => new SearchResponse.FacetCount
                    {
                        Label = Label($"{f.Name}.{v.Value}", searchConfiguration, v.Value),
                        Value = v.Value,
                        Count = v.Count
                    })
                });
        }
    }
}
