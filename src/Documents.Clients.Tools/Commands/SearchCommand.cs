namespace Documents.Clients.Tools.Commands
{
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.Search;
    using Documents.Queues.Messages;
    using McMaster.Extensions.CommandLineUtils;
    using MoreLinq;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    class SearchCommand : CommandBase
    {
        private const int ENQUEUE_BATCH_SIZE = 250;

        [Argument(0, Description = "Key")]
        public string Key { get; }

        [Argument(1, Description = "Query")]
        public string Query { get; }

        [Option]
        public bool Native { get; }

        [Option]
        public string Enqueue { get; }

        [Option]
        public bool Debug { get; }

        [Option]
        public bool Facets { get; set; }

        [Option]
        public bool SimulateUpload { get; set; }

        [Option("--facet-filter", CommandOptionType.MultipleValue)]
        public string[] FacetFilter { get; set; }

        protected async override Task ExecuteAsync()
        {
            var organizationIdentifier = GetOrganizationIdentifier(Key);

            var searchRequest = new SearchRequest
            {
                FolderIdentifier = new FolderIdentifier(organizationIdentifier, null),
                OrganizationIdentifier = organizationIdentifier,
                Paging = new PagingArguments
                {
                    PageSize = 10000
                }
            };

            if (!Native)
                searchRequest.KeywordQuery = Query;
            else
                searchRequest.NativeQuery = Query;

            if (FacetFilter != null && FacetFilter.Any())
            {
                searchRequest.Filters = FacetFilter.Select(f => new Filter
                {
                    Name = f.Split("=")[0]?.Trim(),
                    Value = f.Split("=")[1]?.Trim(),
                });
            }

            var results = await API.Search.SearchAsync(searchRequest);

            if (Debug)
            {
                Console.WriteLine(results.DebugQuery);
                return;
            }

            if (Enqueue != null || SimulateUpload)
            {
                bool done = false;
                int recordCount = 0;

                while (!done)
                {
                    foreach (var batch in results.Rows.Batch(ENQUEUE_BATCH_SIZE))
                    {
                        if (SimulateUpload)
                            await API.Queue.EnqueueAsync(batch.Select(r => new QueuePair
                            {
                                QueueName = "EventRouter",
                                Message = JsonConvert.SerializeObject(new FileContentsUploadCompleteEvent
                                {
                                    FileIdentifier = r.FileIdentifier,
                                    Generated = DateTime.UtcNow
                                })
                            }));
                    }

                    recordCount += results.Rows.Count();

                    if (recordCount < results.TotalMatches)
                    {
                        Console.WriteLine("Advancing a page");
                        searchRequest.Paging.PageIndex++;
                        results = await API.Search.SearchAsync(searchRequest);
                    }
                    else
                        done = true;
                }
            }

            Table("Search Results", results.Rows.Select(r => new
            {
                r.FileIdentifier.OrganizationKey,
                r.FileIdentifier.FolderKey,
                r.FileIdentifier.FileKey,
                r.Name
            }));

            if (Facets)
                Table("Facets", results.Facets.SelectMany(f => f.Values.Select(v => new
                {
                    f.Name,
                    v.Value,
                    v.Count
                })).ToList());

        }

    }
}
