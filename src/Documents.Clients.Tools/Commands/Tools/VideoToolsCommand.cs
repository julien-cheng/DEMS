namespace Documents.Clients.Tools.Commands.Tools
{
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using McMaster.Extensions.CommandLineUtils;
    using MoreLinq;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    [Subcommand("frame", typeof(ExportFrame))]
    [Subcommand("clip", typeof(ExportClip))]
    [Subcommand("watermark", typeof(WatermarkClip))]
    [Subcommand("RequeueEXIF", typeof(Organization))]
    class VideoToolsCommand : CommandBase
    {
        class ExportFrame : CommandBase
        {

            [Required, Argument(0)]
            public string FileIdentifier { get; }

            [Required, Argument(1)]
            public int Millisecond { get; }

            [Required, Argument(2)]
            public string OutputName { get; }

            protected override async Task ExecuteAsync()
            {
                await API.Queue.EnqueueAsync("VideoTools", new VideoToolsMessage
                {
                    Identifier = GetFileIdentifier(FileIdentifier),
                    Frame = new VideoToolsMessage.ExportFrameDetails
                    {
                        StartTimeMS = Millisecond
                    },
                    OutputName = OutputName
                });
            }
        }

        class ExportClip : CommandBase
        {
            [Required, Argument(0)]
            public string FileIdentifier { get; }

            [Required, Argument(1)]
            public string Range { get; }

            [Option]
            public string[] Mute { get; }

            [Required, Argument(2)]
            public string OutputName { get; }

            protected override async Task ExecuteAsync()
            {
                var parts = Range.Split(":");
                var times = parts.Select(p => int.Parse(p)).ToArray();

                var mutedRanges = Mute != null && Mute.Any()
                    ? Mute.Select(m =>
                    {
                        var muteTimes = m.Split(":").Select(p => int.Parse(p)).ToArray();

                        return new VideoToolsMessage.ClippingDetails.MutedRange
                        {
                            StartTimeMS = muteTimes[0],
                            EndTimeMS = muteTimes[1]
                        };
                    }).ToList()
                    : null;

                await API.Queue.EnqueueAsync("VideoTools", new VideoToolsMessage
                {
                    Identifier = GetFileIdentifier(FileIdentifier),
                    Clipping = new VideoToolsMessage.ClippingDetails
                    {
                        StartTimeMS = times[0],
                        EndTimeMS = times[1],
                        MutedRanges = mutedRanges
                    },
                    OutputName = OutputName
                });
            }
        }

        class WatermarkClip : CommandBase
        {
            [Required, Argument(0)]
            public string FileIdentifier { get; }

            [Required, Argument(1)]
            public string WatermarkFileIdentifier { get; }

            [Required, Argument(2)]
            public string OutputName { get; }

            protected override async Task ExecuteAsync()
            {
                await API.Queue.EnqueueAsync("VideoTools", new VideoToolsMessage
                {
                    Identifier = GetFileIdentifier(FileIdentifier),
                    Watermark = new VideoToolsMessage.WatermarkingDetails
                    {
                        Watermark = GetFileIdentifier(WatermarkFileIdentifier)
                    },
                    OutputName = OutputName
                });
            }
        }

        class Organization : CommandBase
        {
            [Required, Argument(0)]
            public string OrganizationIdentifier { get; }

            private const int ENQUEUE_BATCH_SIZE = 250;

            protected override async Task ExecuteAsync()
            {
                var organizationIdentifier = GetOrganizationIdentifier(OrganizationIdentifier);                

                var searchRequest = new SearchRequest
                {
                    FolderIdentifier = new FolderIdentifier(organizationIdentifier, null),
                    OrganizationIdentifier = organizationIdentifier,
                    Paging = new PagingArguments
                    {
                        PageSize = 10000
                    },
                    KeywordQuery = "*"
                };

                var results = await API.Search.SearchAsync(searchRequest);

                foreach (var batch in results.Rows.Batch(ENQUEUE_BATCH_SIZE))
                {
                    await API.Queue.EnqueueAsync(batch.Select(r => new QueuePair
                    {
                        QueueName = "ExifTool",                        
                        Message = JsonConvert.SerializeObject(new FileBasedMessage
                        {
                            Identifier = r.FileIdentifier
                        })
                    }));                    
                }
            }
        }
    }
}
