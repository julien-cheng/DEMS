namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Security;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Queues.Messages;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ClipService : ServiceBase<ClipSet, FileIdentifier>
    {
        private const string FILENAME = "clipset.json";
        private readonly ViewSetService ViewSetService;

        public ClipService(APIConnection connection, ViewSetService viewSetService): base(connection)
        {
            this.ViewSetService = viewSetService;
        }

        public static ClipSet ClipSetGet(OrganizationModel organization, FileModel file)
        {
            return ClipSetGet(organization, file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType);
        }

        public static ClipSet ClipSetGet(OrganizationModel organization, FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType)
        {
            var mediaset = MediaService.MediaSetGet(organization, fileIdentifier, alternativeViews, extension, mimeType);

            var canonicalURL = organization.Read<string>("ClipSetCanonicalURLFormat");
            if (canonicalURL != null)
            {
                var partial = fileIdentifier.FolderKey?.Split(':')?.LastOrDefault();

                canonicalURL = canonicalURL.Replace("__FOLDER_KEY_PARTIAL__", partial);
            }

            return new ClipSet
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
                Segments = null,
                CanonicalURLFormat = canonicalURL
            };
        }

        public async Task DeleteInsertAsync(FileIdentifier identifier, ClipSet clipSetToInsert = null)
        {
            var rootFile = await Connection.File.GetAsync(identifier);

            var oldClipSetJsonIdentifier =
                rootFile.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS)?
                .FirstOrDefault(v => v.Name == FILENAME)?
                .FileIdentifier;

            FileModel newClipSetFile = null;

            if (clipSetToInsert != null)
            {
                newClipSetFile = 
                    new FileModel(new FileIdentifier(rootFile.Identifier as FolderIdentifier, Guid.NewGuid().ToString()))
                    {
                        Name = FILENAME
                    }
                    .InitializeEmptyPrivileges()
                    .InitializeEmptyMetadata()
                    .MetaHiddenWrite(true);

                newClipSetFile.WriteACLs("delete", new[] {
                    new ACLModel
                    {
                        RequiredIdentifiers = new List<string>
                        {
                            "u:system",
                            $"o:{identifier.OrganizationKey}"
                        }
                    }
                });

                newClipSetFile = await Connection.File.UploadAsync(
                    newClipSetFile,
                    JsonConvert.SerializeObject(clipSetToInsert.Segments)
                );
            }

            await Connection.ConcurrencyRetryBlock(async () =>
            {
                // get the current file
                rootFile = await Connection.File.GetAsync(identifier);

                // read alternative versions
                var alternativeViews = rootFile.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS) ?? new List<AlternativeView>();

                // remove any existing json children
                alternativeViews = alternativeViews.Where(v => v.Name != FILENAME).ToList();
                if (clipSetToInsert != null)
                    alternativeViews.Add(new AlternativeView
                    {
                        FileIdentifier = newClipSetFile.Identifier,
                        MimeType = "application/json",
                        Name = FILENAME
                    });

                rootFile.Write(MetadataKeyConstants.ALTERNATIVE_VIEWS, alternativeViews);

                await Connection.File.PutAsync(rootFile);
            });

            if (oldClipSetJsonIdentifier != null)
                await Connection.File.DeleteAsync(oldClipSetJsonIdentifier);
        }

        public async Task RequestTranscriptAsync(TranscriptionRequest transcriptionRequest)
        {
            await Connection.Queue.EnqueueAsync("Voicebase", new FileBasedMessage(transcriptionRequest.FileIdentifier));
        }

        public async override Task<FileIdentifier> DeleteOneAsync(FileIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DeleteInsertAsync(identifier);
            return identifier;
        }

        public async override Task<ClipSet> UpsertOneAsync(ClipSet model, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DeleteInsertAsync(model.RootFileIdentifier, model);
            return await QueryOneAsync(model.RootFileIdentifier);
        }

        public async override Task<ClipSet> QueryOneAsync(FileIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            var rootFile = await Connection.File.GetAsync(identifier);

            var clipSetJson =
                rootFile.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS)
                ?.FirstOrDefault(v => v.Name == FILENAME);

            var set = await ViewSetService.LoadSet<ClipSet>(identifier);

            if (clipSetJson != null)
                set.Segments = await Connection.File.DownloadAsAsync<List<ClipSegmentModel>>(clipSetJson.FileIdentifier);

            var ops = set.AllowedOperations.ToList();

            ops.Add(AllowedOperation.GetAllowedOperationExportFrame(identifier));
            ops.Add(AllowedOperation.GetAllowedOperationExportClip(identifier));

            set.AllowedOperations = ops;

            return set;
        }

        public override Task<IEnumerable<ClipSet>> QueryAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task ExportClipAsync(ExportClipRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                var file = await Connection.File.GetAsync(request.FileIdentifier);
                var childOf = file.Read<FileIdentifier>("_childof");
                if (childOf != null)
                    file = await Connection.File.GetAsync(childOf);

                request.FileName = $"{file.NameWithoutExtension()}.Clip.mp4";
            }
            else
            {
                if (!request.FileName.ToLower().EndsWith(".mp4"))
                    request.FileName += ".mp4";
            }

            await Connection.Queue.EnqueueAsync("VideoTools", new VideoToolsMessage
            {
                Identifier = request.FileIdentifier,
                OutputName = request.FileName,
                Clipping = new VideoToolsMessage.ClippingDetails
                {
                    StartTimeMS = request.Clip.StartTime,
                    EndTimeMS = request.Clip.EndTime,
                    MutedRanges = request.Clip.Mutes.Select(m => new VideoToolsMessage.ClippingDetails.MutedRange
                    {
                        StartTimeMS = m.StartTime,
                        EndTimeMS = m.EndTime
                    }).ToList()
                }
            });
        }

        public async Task ExportFrameAsync(ExportFrameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                var file = await Connection.File.GetAsync(request.FileIdentifier);
                var childOf = file.Read<FileIdentifier>("_childof");
                if (childOf != null)
                    file = await Connection.File.GetAsync(childOf);

                request.FileName = $"{file.NameWithoutExtension()}.Frame.png";
            }
            else
            {
                if (!request.FileName.ToLower().EndsWith(".png"))
                    request.FileName += ".png";
            }

            await Connection.Queue.EnqueueAsync("VideoTools", new VideoToolsMessage
            {
                Identifier = request.FileIdentifier,
                OutputName = request.FileName,
                Frame = new VideoToolsMessage.ExportFrameDetails
                {
                    StartTimeMS = request.Milliseconds
                }
            });
        }
    }
}
