namespace Documents.Search.ElasticSearch
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Models.Search;
    using Elasticsearch.Net;
    using Microsoft.Extensions.Logging;
    using Nest;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ElasticSearchDriver : ISearch
    {
        public volatile static List<string> ExtantIndicies = new List<string>();

        private ElasticClient client;

        private ElasticLowLevelClient lowLevelClient;

        private string IndexName = null;
        private ILogger<ElasticSearchDriver> Logger = null;

        public ElasticSearchDriver(ILogger<ElasticSearchDriver> logger, Uri uri, string index)
        {
            IndexName = index;
            Logger = logger;


            var connectionSettings = new ConnectionSettings(uri)
                .DefaultIndex(index)
                .ThrowExceptions();

            lowLevelClient = new ElasticLowLevelClient(connectionSettings);
            client = new ElasticClient(connectionSettings);

            if (!ExtantIndicies.Contains(index))
            {
                lock (ExtantIndicies)
                {
                    var exists = client.IndexExists(index);
                    if (!exists.Exists)
                    {
                        Logger.LogInformation("Index does not exist, creating it now");

                        var descriptor = new CreateIndexDescriptor(index)
                        .Mappings(ms => ms
                            .Map<Document>(m => m.AutoMap())
                        );

                        var response = client.CreateIndex(descriptor);

                    }
                    ExtantIndicies.Add(index);
                }
            }
        }

        QueryContainer DoSecurity(QueryContainerDescriptor<Document> q, string[] securityIdentifiers)
        {
            var identifiers = "_all " + string.Join(" ", 
                    securityIdentifiers.Select(i => PreprocessIdentifier(i)
                )
                ?? new string[0]);

            return q.Bool(b => b
                .Filter(
                    f => f.Match(m => m.Field(x => x.ACL_0).Query(identifiers)),
                    f => f.Match(m => m.Field(x => x.ACL_1).Query(identifiers)),
                    f => f.Match(m => m.Field(x => x.ACL_2).Query(identifiers)),
                    f => f.Match(m => m.Field(x => x.ACL_3).Query(identifiers)),
                    f => f.Match(m => m.Field(x => x.ACL_4).Query(identifiers))
                )
            );
        }

        QueryContainer DoFacetFiltering(QueryContainerDescriptor<Document> q, IEnumerable<Filter> facetFilters)
        {
            if (facetFilters == null)
                return null;

            var filters = new List<Func<QueryContainerDescriptor<Document>, QueryContainer>>();

            foreach (var filter in facetFilters)
            {
                filters.Add(f => f.Nested(n => n
                    .Path(p => p.FacetStrings)
                    .Query(q2 => q2
                        .Bool(b => b
                            .Filter(
                                f2 => f2.Term("facets.name", filter.Name),
                                f2 => f2.Term("facets.value", filter.Value)
                            )
                        )
                    )
                ));
            }

            return q.Bool(b => b
                .Filter(filters.ToArray())
            );
        }

        QueryContainer DoFolderFiltering(QueryContainerDescriptor<Document> q, API.Common.Models.SearchRequest searchRequest)
        {
            var filters = new List<Func<QueryContainerDescriptor<Document>, QueryContainer>> {
                { f => f.Term(d => d.OrganizationKey, searchRequest.OrganizationIdentifier.OrganizationKey) }
            };
            if (searchRequest.FolderIdentifier?.FolderKey != null)
                filters.Add(f => f.Term(d => d.FolderKey, searchRequest.FolderIdentifier.FolderKey));

            return q.Bool(b => b
                .Filter(filters.ToArray())
            );
        }

        private string UniqueIdentifierFromFileIdentifier(FileIdentifier fileIdentifier)
        {
            return $"{fileIdentifier.OrganizationKey}|{fileIdentifier.FolderKey}|{fileIdentifier.FileKey}";
        }

        async Task ISearch.DeleteFileAsync(
            string[] securityIdentifiers,
            FileIdentifier fileIdentifier
        )
        {
            Logger.LogInformation($"Delete {fileIdentifier}");
            string uniqueKey = UniqueIdentifierFromFileIdentifier(fileIdentifier);

            await client.DeleteByQueryAsync<Document>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Filter(
                            f => f.Term(d => d.UniqueKey, uniqueKey),
                            f => DoSecurity(f, securityIdentifiers)
                        )
                    )
                )
            );
        }

        async Task ISearch.DeleteFolderAsync(
            string[] securityIdentifiers,
            FolderIdentifier folderIdentifier
        )
        {
            Logger.LogInformation($"Delete {folderIdentifier}");
            await client.DeleteByQueryAsync<Document>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Filter(
                            f => f.Term(d => d.OrganizationKey, folderIdentifier.OrganizationKey),
                            f => f.Term(d => d.FolderKey, folderIdentifier.FolderKey),
                            f => DoSecurity(f, securityIdentifiers)
                        )
                    )
                )
            );
        }

        async Task ISearch.DeleteOrganizationAsync(
            string[] securityIdentifiers,
            OrganizationIdentifier organizationIdentifier
        )
        {
            Logger.LogInformation($"Delete {organizationIdentifier}");
            var response = await client.DeleteByQueryAsync<Document>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Filter(
                            f => f.Term(d => d.OrganizationKey, organizationIdentifier.OrganizationKey),
                            f => DoSecurity(f, securityIdentifiers)
                        )
                    )
                )
            );
        }

        private List<FacetString> FacetsFromMetadata(FolderModel folderModel, FileModel fileModel)
        {
            var facets = new List<FacetString>();
            var attributeLocators = folderModel.Read<List<AttributeLocator>>(MetadataKeyConstants.ATTRIBUTE_LOCATORS);
            if (attributeLocators != null)
                foreach (var locator in attributeLocators)
                {
                    if (locator.IsFacet)
                    {
                        var value = fileModel.Read<string>(locator.Key)
                            ?? folderModel.Read<string>(locator.Key);

                        facets.Add(new FacetString
                        {
                            Name = locator.Key,
                            Value = value
                        });
                    }
                }

            var extension = fileModel.Extension;
            if (extension != null && extension.Length > 4)
                extension = null;

            facets.Add(new FacetString { Name = "extension", Value = extension });

            return facets;
        }

        private Dictionary<string, string> FieldsFromMetadata(FolderModel folderModel, FileModel fileModel)
        {
            var fields = new Dictionary<string, string>();
            var attributeLocators = folderModel.Read<List<AttributeLocator>>(MetadataKeyConstants.ATTRIBUTE_LOCATORS);
            if (attributeLocators != null)
                foreach (var locator in attributeLocators)
                {
                    if (locator.IsIndexed)
                    {
                        try
                        {
                            var value = fileModel.Read<string>(locator.Key)
                                ?? folderModel.Read<string>(locator.Key);

                            fields.Add(locator.Key, value);
                        }
                        catch (Exception)
                        {
                            // well... we tried
                        }
                    }
                }

            return fields;
        }

        private string GetNakedExtension(string name)
        {
            var extension = Path.GetExtension(name);
            if (extension != null && extension.StartsWith('.'))
                extension = extension.Substring(1);

            return extension;
        }

        private string PreprocessIdentifier(string identifier)
        {
            return identifier;
        }

        async Task ISearch.IndexFileAsync(
            FolderModel folderModel,
            FileModel fileModel,
            string text
        )
        {
            Logger.LogInformation($"Index {fileModel.Identifier}");
            string uniqueKey = UniqueIdentifierFromFileIdentifier(fileModel.Identifier);

            var document = new Document
            {
                UniqueKey = uniqueKey,
                Indexed = DateTime.UtcNow,

                OrganizationKey = fileModel.Identifier.OrganizationKey,
                FolderKey = fileModel.Identifier.FolderKey,
                FileKey = fileModel.Identifier.FileKey,

                Type = "File",

                Name = fileModel.Name,
                Extension = GetNakedExtension(fileModel.Name),
                MimeType = fileModel.MimeType,

                Fields = FieldsFromMetadata(folderModel, fileModel),
                FacetStrings = FacetsFromMetadata(folderModel, fileModel),
                Metadata = fileModel.MetadataFlattened,

                Created = fileModel.Created,
                Modified = fileModel.Modified,

                Content = text,
            };

            string buildACLSet(ACLModel[] acls, int index)
            {
                return (acls.Count() > index && acls[index].RequiredIdentifiers.Any())
                    ? string.Join(" ", acls[index].RequiredIdentifiers.Select(i => PreprocessIdentifier(i)))
                    : "_all";

            }

            var privilege = folderModel.PrivilegesFlattened.Where(f => f.Key == "read").Select(f => f.Value).FirstOrDefault();
            if (privilege != null)
            {
                var acls = privilege.ToArray();
                document.ACL_0 = buildACLSet(acls, 0);
                document.ACL_1 = buildACLSet(acls, 1);
                document.ACL_2 = buildACLSet(acls, 2);
                document.ACL_3 = buildACLSet(acls, 3);
                document.ACL_4 = buildACLSet(acls, 4);

                /*document.ACL_0 = (acls.Count() > 0 && acls[0].RequiredIdentifiers.Any()) ? string.Join(" ", acls[0].RequiredIdentifiers) : "_all";
                document.ACL_1 = (acls.Count() > 1 && acls[1].RequiredIdentifiers.Any()) ? string.Join(" ", acls[1].RequiredIdentifiers) : "_all";
                document.ACL_2 = (acls.Count() > 2 && acls[2].RequiredIdentifiers.Any()) ? string.Join(" ", acls[2].RequiredIdentifiers) : "_all";
                document.ACL_3 = (acls.Count() > 3 && acls[3].RequiredIdentifiers.Any()) ? string.Join(" ", acls[3].RequiredIdentifiers) : "_all";
                document.ACL_4 = (acls.Count() > 4 && acls[4].RequiredIdentifiers.Any()) ? string.Join(" ", acls[4].RequiredIdentifiers) : "_all";*/
            }
            else
            {
                document.ACL_0 =
                document.ACL_1 =
                document.ACL_2 =
                document.ACL_3 =
                document.ACL_4 = "_all";
            }

            var response = await client.IndexDocumentAsync(document);
        }

        Func<QueryContainerDescriptor<Document>, QueryContainer>[] DoQueries(QueryContainerDescriptor<Document> q, API.Common.Models.SearchRequest searchRequest)
        {
            var queries = new List<Func<QueryContainerDescriptor<Document>, QueryContainer>>();

            if (searchRequest.KeywordQuery != null)
            {
                queries.Add(query => query
                    .Wildcard(w => w
                    .Boost(1.1)
                    .Field(field => field.Name)
                    .Value($"*{searchRequest.KeywordQuery}*")
                ));

                queries.Add(query => query
                    .Match(m => m
                    .Field(f => f.Content)
                    .Query(searchRequest.KeywordQuery)
                ));
            }

            if (searchRequest.NativeQuery != null)
            {
                queries.Add(query => query
                    .QueryString(qs => qs
                        .Query(searchRequest.NativeQuery)
                    )
                );
            }


            return queries.ToArray();
        }

        async Task<ISearchResults> ISearch.Search(
            string[] securityIdentifiers,
            API.Common.Models.SearchRequest searchRequest)
        {
            var query = new
            {
                from = searchRequest.Paging.PageIndex * searchRequest.Paging.PageSize,
                size = searchRequest.Paging.PageSize,
                sort = new[]
                {
                    new { _score = new { order = "desc" } }
                },
                highlight = new
                {
                    pre_tags = new[] { "<em>" },
                    post_tags = new[] { "</em>" },
                    fields = new
                    {
                        content = new
                        {
                            number_of_fragments = 3,
                            boundary_max_scan = 50,
                            type = "fvh"
                        }
                    }
                },
                aggs = new
                {
                    facet_strings = new
                    {
                        nested = new { path = "facets" },
                        aggs = new
                        {
                            facet_name = new
                            {
                                terms = new { field = "facets.name" },
                                aggs = new
                                {
                                    facet_value = new { terms = new { field = "facets.value", size = 10 } }
                                }
                            }
                        }
                    }
                },
                _source = new[] {
                    "uniqueKey",
                    "indexed",
                    "organizationKey",
                    "folderKey",
                    "fileKey",
                    "type",
                    "name",
                    "extension",
                    "mimeType",
                    "created",
                    "modified",
                    "mimeType",
                    "fields",
                    "metadata",
                    "facets"
                },
                query = new
                {
                    @bool = new
                    {
                        should = UserQuery(searchRequest),
                        filter = OrganizationAndFolderFilter(searchRequest.OrganizationIdentifier, searchRequest.FolderIdentifier)
                            .Append(SecurityFilters(securityIdentifiers))
                            .Append(NestedFacetFilters(searchRequest)),
                        minimum_should_match = 1
                    }
                }
            };


            var queryJSON = JsonConvert.SerializeObject(query);
            var searchResponse = await lowLevelClient.SearchAsync<StringResponse>(IndexName, PostData.String(queryJSON));

            var deserialized = JsonConvert.DeserializeObject<SearchResposne>(searchResponse.Body);

            var searchResults = new SearchResults
            {
                Rows = deserialized.Hits.Hits.Select(d => new SearchResult
                {
                    FileIdentifier = new FileIdentifier
                    {
                        OrganizationKey = d._Source.OrganizationKey,
                        FolderKey = d._Source.FolderKey,
                        FileKey = d._Source.FileKey
                    },

                    Name = d._Source.Name,
                    Extension = d._Source.Extension,
                    MimeType = d._Source.MimeType,

                    Metadata = d._Source.Metadata,

                    Created = d._Source.Created,
                    Modified = d._Source.Modified,
                    Highlights = d.Highlight?.SelectMany(h => h.Value).ToArray(),

                    Fields = d._Source.Fields.Where(f => !String.IsNullOrWhiteSpace(f.Value)).ToDictionary(f => f.Key, f => f.Value)

                }),

                TotalMatches = deserialized.Hits.Total,
                DebugQuery = queryJSON
            };

            var facetModels = new List<FacetModel>();

            foreach (var facet in deserialized.Aggregations.Facet_Strings.Facet_Name.Buckets)
            {
                var model = new FacetModel
                {
                    Name = facet.Key.ToString()   
                };

                var values = new List<FacetValue>();
                model.Values = values;

                foreach (var facetValue in facet.Facet_Value.Buckets)
                {
                    values.Add(new FacetValue
                    {
                        Value = facetValue.Key.ToString(),
                        Count = facetValue.Doc_Count
                    });
                }

                if (model.Values.Any() && facet.Facet_Value.Sum_Other_Doc_Count == 0)
                {
                    if (model.Values.Count() == 1
                        && model.Values.First().Count == deserialized.Hits.Total)
                    {
                        // this is a single facet describing all 
                        // results. it doesn't further differentiate the resultset.

                        // skip it
                    }
                    else
                        facetModels.Add(model);
                }

            }
            searchResults.Facets = facetModels;

            return searchResults;
        }

        private IEnumerable<object> OrganizationAndFolderFilter(OrganizationIdentifier organizationIdentifier, FolderIdentifier folderIdentifier)
        {
            if (organizationIdentifier?.IsValid ?? false)
                yield return new
                {
                    term = new
                    {
                        organizationKey = new
                        {
                            value = organizationIdentifier.OrganizationKey
                        }
                    }
                };

            if (folderIdentifier?.IsValid ?? false)
                yield return new
                {
                    term = new
                    {
                        folderKey = new
                        {
                            value = folderIdentifier.FolderKey
                        }
                    }
                };
        }

        private IEnumerable<object> SecurityFilters(string[] securityIdentifiers)
        {
            var identifiers = string.Join(' ', securityIdentifiers
                .Select(i => PreprocessIdentifier(i))
                .Append("_all"));

            return new object[]
            {
                new { match = new { aCL_0 =  new { query = identifiers } } },
                new { match = new { aCL_1 =  new { query = identifiers } } },
                new { match = new { aCL_2 =  new { query = identifiers } } },
                new { match = new { aCL_3 =  new { query = identifiers } } },
                new { match = new { aCL_4 =  new { query = identifiers } } }
            };
        }

        private IEnumerable<object> NestedFacetFilters(API.Common.Models.SearchRequest searchRequest)
        {
            return searchRequest?.Filters?.Select(f => NestedFacetFilter(f.Name, f.Value))
                ?? new object[0];
        }

        private object NestedFacetFilter(string name, string value)
        {
            return new
            {
                nested = new
                {
                    query = new
                    {
                        @bool = new
                        {
                            filter = new object[]
                            {
                                new
                                {
                                    term = new Dictionary<string, object>
                                    {
                                        { "facets.name", new { value = name} }
                                    }
                                },
                                new
                                {
                                    term = new Dictionary<string, object>
                                    {
                                        { "facets.value", new { value = value } }
                                    }
                                }
                            }
                        }
                    },
                    path = "facets"
                }
            };
        }

        IEnumerable<object> UserQuery(API.Common.Models.SearchRequest searchRequest)
        {
            if (searchRequest.KeywordQuery != null)
            {
                yield return new
                {
                    wildcard = new
                    {
                        name = new
                        {
                            boost = 1.1,
                            value = $"*{searchRequest.KeywordQuery}*"
                        }
                    }
                };
                yield return new
                {
                    match = new
                    {
                        content = new
                        {
                            query = searchRequest.KeywordQuery
                        }
                    }
                };
            }

            if (searchRequest.NativeQuery != null)
                yield return new
                {
                    query_string = new
                    {
                        query = searchRequest.NativeQuery
                    }
                };
        }

        async Task<ISearchResults> OldSearch(
            string[] securityIdentifiers,
            API.Common.Models.SearchRequest searchRequest)
        {

            Logger.LogInformation($"Search {searchRequest.FolderIdentifier} \"{searchRequest}\"");

            var identifiers = "_all " + string.Join(" ", securityIdentifiers ?? new string[0]);

            int FacetLimit = 10;

            SearchDescriptor<Document> searchDescriptor = null;
            var sortField = searchRequest.Paging.SortField;
            searchDescriptor = new SearchDescriptor<Document>()
                .Query(q => q
                    .Bool(b => b
                        .MinimumShouldMatch(1)
                        .Filter(
                            f => DoFolderFiltering(f, searchRequest),
                            f => DoSecurity(f, securityIdentifiers),
                            f => DoFacetFiltering(f, searchRequest.Filters)
                        )
                        .Should(DoQueries(q, searchRequest))
                    )
                )
                .Highlight(h => h
                    .PreTags("<em>")
                    .PostTags("</em>")
                    .Fields(fs => fs
                        .Field(p => p.Content)
                        .Type(HighlighterType.Fvh)
                        .BoundaryMaxScan(50)
                        .NumberOfFragments(3)
                    )
                )
                .From(searchRequest.Paging.PageIndex * searchRequest.Paging.PageSize)
                .Size(searchRequest.Paging.PageSize == 0
                    ? 500
                    : searchRequest.Paging.PageSize)
                .Sort(ss => ss
                    .Field(f =>
                    {
                        f.Order(searchRequest.Paging.IsAscending
                            ? SortOrder.Ascending
                            : SortOrder.Descending);

                        switch (searchRequest.Paging.SortField)
                        {
                            case "name":
                                f.Field(ff => ff.Name);
                                break;
                            default:
                                f.Field("_score");
                                f.Descending();
                                break;
                        }

                        return f;
                    })
                )
                .Aggregations(aggs => aggs
                    .Nested("facet_strings", n => n
                        .Path(p => p.FacetStrings)
                        .Aggregations(aa => aa
                            .Terms("facet_name", t => t
                                .Field(new Field("facets.name"))
                                .Aggregations(a2 => a2
                                    .Terms("facet_value", t2 => t2
                                        .Field(new Field("facets.value"))
                                        .Size(FacetLimit)
                                    )
                                )
                            )
                        )
                    )
                )
                ;


            string debugQuery = SerializeNESTObject(searchDescriptor);

            Logger.LogDebug($"Search Query: (index: {IndexName})\n {debugQuery}");

            var results = await client.SearchAsync<Document>(searchDescriptor);

            //Logger.LogDebug($"Raw Results: {SerializeNESTObject(results)}");

            var searchResults = new SearchResults
            {
                Rows = results.Hits.Select(d => new SearchResult
                {
                    FileIdentifier = new FileIdentifier
                    {
                        OrganizationKey = d.Source.OrganizationKey,
                        FolderKey = d.Source.FolderKey,
                        FileKey = d.Source.FileKey
                    },

                    Name = d.Source.Name,
                    Extension = d.Source.Extension,
                    MimeType = d.Source.MimeType,

                    Metadata = d.Source.Metadata,

                    Created = d.Source.Created,
                    Modified = d.Source.Modified,
                    Highlights = d.Highlights.SelectMany(h => h.Value.Highlights).ToArray(),

                    Fields = d.Source.Fields

                }),

                TotalMatches = results.Total,
                DebugQuery = debugQuery
            };

            var facetModels = new List<FacetModel>();

            if ((results.Aggregations.FirstOrDefault(a => a.Key == "facet_strings")
                .Value as SingleBucketAggregate)
                ?.FirstOrDefault()
                .Value is BucketAggregate facets)
                foreach (var facet in facets.Items.OfType<KeyedBucket<object>>())
                {
                    var model = new FacetModel
                    {
                        Name = facet.Key.ToString()
                    };

                    var values = new List<FacetValue>();
                    model.Values = values;

                    foreach (var container in facet.Values.OfType<BucketAggregate>())
                    {
                        foreach (var facetValue in container.Items.OfType<KeyedBucket<object>>())
                        {
                            values.Add(new FacetValue
                            {
                                Value = facetValue.Key.ToString(),
                                Count = facetValue.DocCount
                            });
                        }

                        if (model.Values.Any() && container.SumOtherDocCount == 0)
                        {
                            if (model.Values.Count() == 1
                                && model.Values.First().Count == results.Total)
                            {
                                // this is a single facet describing all 
                                // results. it doesn't further differentiate the resultset.

                                // skip it
                            }
                            else
                                facetModels.Add(model);
                        }
                    }

                }
            searchResults.Facets = facetModels;

            return searchResults;
        }

        private string SerializeNESTObject(object obj)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                client.RequestResponseSerializer.Serialize(obj, mStream);
                return Encoding.ASCII.GetString(mStream.ToArray());
            }
        }

        // deletes the entire index
        // this includes all organizations, folders, files
        // ... however, there may be more than one index.
        async Task ISearch.DeleteEntireIndex()
        {
            await client.DeleteIndexAsync(Indices.Index(IndexName));
        }
    }
}
