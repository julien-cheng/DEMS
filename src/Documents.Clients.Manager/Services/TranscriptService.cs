namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Security;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Common.Subtitles;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Queues.Messages;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class TranscriptService : ServiceBase<TranscriptSet, FileIdentifier>
    {
        public const string FILENAME_JSON = "transcript.json";
        public const string FILENAME_VTT = "transcript.vtt";

        private readonly ViewSetService ViewSetService;

        public TranscriptService(APIConnection connection, ViewSetService viewSetService): base(connection)
        {
            this.ViewSetService = viewSetService;
        }

        public static TranscriptSet TranscriptSetGet(OrganizationModel organization, FileModel file)
        {
            return TranscriptSetGet(organization, file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType);
        }

        public static TranscriptSet TranscriptSetGet(OrganizationModel organization, FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType)
        {
            var mediaset = MediaService.MediaSetGet(organization, fileIdentifier, alternativeViews, extension, mimeType);

            return new TranscriptSet
            {
                AutoPlay = mediaset.AutoPlay,
                MediaType = mediaset.MediaType,
                Poster = mediaset.Poster,
                Preload = mediaset.Preload,
                RootFileIdentifier = mediaset.RootFileIdentifier,
                Sources = mediaset.Sources,
                Subtitles = mediaset.Subtitles,
                Views = mediaset.Views,
                AllowedOperations = mediaset.AllowedOperations,
                Segments = null
            };
        }

        public async static Task<IEnumerable<SegmentModel>> LoadSegments(APIConnection api, FileIdentifier identifier)
        {
            var parser = new VTTFormat();

            using (var ms = new MemoryStream())
            {
                IEnumerable<SegmentModel> segments = null;

                await api.File.DownloadAsync(identifier, async (stream, cancel) =>
                {
                    var items = await parser.ParseStreamAsync(stream);

                    segments = items.Select(i => {
                        var text = i.Lines != null ? string.Join('\n', i.Lines) : string.Empty;
                        return new SegmentModel
                        {
                            StartTime = (int)i.StartTime.TotalMilliseconds,
                            EndTime = (int)i.EndTime.TotalMilliseconds,
                            Text = text,
                            TextOriginal = text
                        };
                    });
                });

                return segments;
            }
        }

        private void SetPrivileges(FileModel file)
        {
            file.WriteACLs("delete", new[] {
                new ACLModel
                {
                    RequiredIdentifiers = new List<string>
                    {
                        "u:system",
                        $"o:{file.Identifier.OrganizationKey}"
                    }
                }
            });
        }

        private FileIdentifier FindViewByName(FileModel file, string viewName)
        {
            return 
                file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS)?
                .FirstOrDefault(v => v.Name == viewName)?
                .FileIdentifier;
        }

        private async Task<FileModel> CreateHiddenChildFileAsync(FileIdentifier fileIdentifier, string newName, string contents, string mimeType)
        {
            var newFile =
                new FileModel(new FileIdentifier(fileIdentifier as FolderIdentifier, Guid.NewGuid().ToString()))
                {
                    Name = newName,
                    MimeType = mimeType
                }
                .InitializeEmptyMetadata()
                .InitializeEmptyPrivileges()
                .MetaHiddenWrite(true)
                .MetaChildOfWrite(fileIdentifier);

            SetPrivileges(newFile);

            newFile = await Connection.File.UploadAsync(
                newFile,
                contents
            );


            return newFile;
        }

        private void ReplaceViewByName(FileModel parentFile, string name, string mimeType, FileIdentifier fileIdentifier)
        {
            // read alternative versions
            var alternativeViews = parentFile.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS) ?? new List<AlternativeView>();

            // remove any existing transcript.json children
            alternativeViews = alternativeViews.Where(v => v.Name != name).ToList();
            if (fileIdentifier != null)
                alternativeViews.Add(new AlternativeView
                {
                    FileIdentifier = fileIdentifier,
                    MimeType = mimeType,
                    Name = name
                });

            parentFile.Write(MetadataKeyConstants.ALTERNATIVE_VIEWS, alternativeViews);
        }

        public async Task DeleteInsertAsync(FileIdentifier identifier, TranscriptSet transcriptSetToInsert = null)
        {
            var rootFile = await Connection.File.GetAsync(identifier);

            var oldTranscriptJsonIdentifier = FindViewByName(rootFile, FILENAME_JSON);
            var oldTranscriptVttIdentifier = FindViewByName(rootFile, FILENAME_VTT);

            FileModel newTranscriptJSONFile = null;
            FileModel newTranscriptVTTFile = null;

            if (transcriptSetToInsert != null)
            {
                newTranscriptJSONFile = await CreateHiddenChildFileAsync(
                    rootFile.Identifier,
                    FILENAME_JSON,
                    JsonConvert.SerializeObject(transcriptSetToInsert.Segments),
                    "application/json"
                );

                var vttFormat = new VTTFormat();

                int segmentIndexNumber = 0;
                var vttStringContent = vttFormat.CreateVTT(transcriptSetToInsert.Segments
                        .Select(s => new SubtitleSegmentModel
                        {
                            StartTime = TimeSpan.FromMilliseconds(s.StartTime),
                            EndTime = TimeSpan.FromMilliseconds(s.EndTime),
                            Lines = s.Text?.Split('\n').ToList(),
                            SegmentIndex = ++segmentIndexNumber
                        }).ToList()
                    );

                newTranscriptVTTFile = await CreateHiddenChildFileAsync(
                    rootFile.Identifier,
                    FILENAME_VTT,
                    vttStringContent,
                    "text/vtt"
                );
            }

            await Connection.ConcurrencyRetryBlock(async () =>
            {
                // get the current file
                rootFile = await Connection.File.GetAsync(identifier);

                ReplaceViewByName(rootFile, FILENAME_JSON, "application/json", newTranscriptJSONFile.Identifier);
                ReplaceViewByName(rootFile, FILENAME_VTT, "text/vtt", newTranscriptVTTFile.Identifier);

                await Connection.File.PutAsync(rootFile);
            });

            if (oldTranscriptJsonIdentifier != null)
                await Connection.File.DeleteAsync(oldTranscriptJsonIdentifier);
            if (oldTranscriptVttIdentifier != null)
                await Connection.File.DeleteAsync(oldTranscriptVttIdentifier);
        }

        public async Task RequestTranscriptAsync(TranscriptionRequest transcriptionRequest)
        {
            await Connection.ConcurrencyRetryBlock(async () =>
            {
                var file = await Connection.File.GetAsync(transcriptionRequest.FileIdentifier);
                file.Write("attribute.requestedBy", Connection.UserIdentifier);
                await Connection.File.PutAsync(file);
            });

            await Connection.Queue.EnqueueAsync("Voicebase", new FileBasedMessage(transcriptionRequest.FileIdentifier));
        }

        public async override Task<FileIdentifier> DeleteOneAsync(FileIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DeleteInsertAsync(identifier);
            return identifier;
        }

        public async override Task<TranscriptSet> UpsertOneAsync(TranscriptSet model, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DeleteInsertAsync(model.RootFileIdentifier, model);
            return await QueryOneAsync(model.RootFileIdentifier);
        }

        public async override Task<TranscriptSet> QueryOneAsync(FileIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            var rootFile = await Connection.File.GetAsync(identifier);

            var transcriptJson =
                rootFile.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS)
                .FirstOrDefault(v => v.Name == FILENAME_JSON);

            var set = await ViewSetService.LoadSet<TranscriptSet>(identifier);

            if (transcriptJson != null)
                set.Segments = await Connection.File.DownloadAsAsync<List<SegmentModel>>(transcriptJson.FileIdentifier);
            else
            {
                var vttIdentifier = set.Subtitles?.FirstOrDefault()?.FileIdentifier;
                set.Segments = await TranscriptService.LoadSegments(Connection, vttIdentifier);
            }

            return set;
        }

        public override Task<IEnumerable<TranscriptSet>> QueryAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
