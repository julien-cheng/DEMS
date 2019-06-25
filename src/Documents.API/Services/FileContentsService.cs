namespace Documents.API.Services
{
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Events;
    using Documents.API.Exceptions;
    using Documents.API.Models;
    using Documents.API.Private;
    using Documents.Store;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class FileContentsService
    {
        private const int CHUNK_SIZE = 10 * 1024 * 1024;

        private readonly IFileStore FileStore;
        private readonly IUploadStore UploadStore;
        private readonly IUploadChunkStore UploadChunkStore;
        private readonly IBackendClient BackendClient;
        private readonly IEventSender EventSender;
        private readonly IOrganizationPrivateMetadata OrganizationPrivateMetadata;
        private readonly ISecurityContext SecurityContext;
        private readonly ILogger<FileContentsService> Logger;

        public FileContentsService(
            IFileStore fileStore,
            IUploadStore uploadStore,
            IUploadChunkStore uploadChunkStore,
            IBackendClient backendClient,
            IEventSender eventSender,
            IOrganizationPrivateMetadata organizationPrivateMetadata,
            ISecurityContext securityContext,
            ILogger<FileContentsService> logger
        )
        {
            this.FileStore = fileStore;
            this.UploadStore = uploadStore;
            this.UploadChunkStore = uploadChunkStore;
            this.BackendClient = backendClient;
            this.EventSender = eventSender;
            this.OrganizationPrivateMetadata = organizationPrivateMetadata;
            this.SecurityContext = securityContext;
            this.Logger = logger;
        }

        private async Task<BackendConfiguration> LoadConfigurationAsync(OrganizationIdentifier organizationIdentifier)
        {
            var privateFolder = await OrganizationPrivateMetadata.PrivateFolderLoadAsync(organizationIdentifier);
            var configuration = privateFolder.Read<BackendConfiguration>(MetadataKeyConstants.BACKEND_CONFIGURATION);

            if (configuration == null)
                throw new ConfigurationException($"{organizationIdentifier} does not have backend configuration");

            return configuration;
        }

        public Task<int> UploadChunkSizeGetAsync()
        {
            return Task.FromResult(CHUNK_SIZE);
        }

        public async Task DownloadAsync(FileModel fileModel, Stream output, long from = 0, long to = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (to == 0)
                to = fileModel.Length - 1;

            var fileLocator = await FileStore.FileLocatorGetAsync(fileModel.Identifier);

            var evt = new FileDownloadEvent
            {
                FileIdentifier = fileModel.Identifier
            };
            evt.Populate(fileModel);

            await EventSender.SendAsync(evt);

            await BackendClient.ReadFileAsync(
                await LoadConfigurationAsync(fileModel.Identifier as OrganizationIdentifier),
                fileLocator,
                output,
                from,
                to,
                fileModel.Length,
                cancellationToken
            );
        }

        public async Task<UploadContextModel> UploadBeginAsync(FileModel fileModel)
        {
            fileModel = await FileStore.UpdateAsync(fileModel);
            await FileStore.UploadingStatusSetAsync(fileModel.Identifier, true);

            var fileLocator = Guid.NewGuid().ToString();
            await FileStore.FileLocatorSetAsync(fileModel.Identifier, fileLocator);

            var uploadModel = await UploadStore.InsertAsync(new UploadModel
            {
                Identifier = new UploadIdentifier
                {
                    UploadKey = await BackendClient.StartChunkedUploadAsync(
                        await LoadConfigurationAsync(fileModel.Identifier as OrganizationIdentifier),
                        fileLocator,
                        new ChunkedUploadModel
                        {
                            Name = fileModel.Name,
                            Length = fileModel.Length,
                            ContentType = fileModel.MimeType
                        }
                    ),
                    OrganizationKey = fileModel.Identifier.OrganizationKey,
                    FolderKey = fileModel.Identifier.FolderKey,
                    FileKey = fileModel.Identifier.FileKey
                },
                Length = fileModel.Length
            });

            int chunkSize = CHUNK_SIZE;
            int totalChunks = (int)(fileModel.Length / chunkSize)
                + ((fileModel.Length % chunkSize == 0)
                    ? 0 // was exact multiple of chunk size
                    : 1);  // plus one for the partial chunk

            return new UploadContextModel
            {
                TotalChunks = totalChunks,
                ChunkSize = chunkSize,
                UploadToken = new UploadTokenModel
                {
                    Identifier = uploadModel.Identifier
                }.ToString(),
                SequentialState = "BEGIN",
                FileLength = fileModel.Length
            };
        }

        public async Task<UploadContextModel> UploadChunkAsync(
            UploadContextModel uploadContext,
            long from,
            long to,
            Stream inputStream,
            int chunkIndex,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var uploadStateToken = UploadTokenModel.Parse(uploadContext.UploadToken);

            var fileLocator = await FileStore.FileLocatorGetAsync(uploadStateToken.Identifier);

            var uploadChunk = new UploadChunkModel
            {
                Identifier = new UploadChunkIdentifier(uploadStateToken.Identifier, fileLocator + '.' + chunkIndex.ToString()),
                ChunkIndex = chunkIndex,
                PositionFrom = from,
                PositionTo = to
            };

            uploadContext.SequentialState = await BackendClient.UploadChunkAsync(
                await LoadConfigurationAsync(uploadStateToken.Identifier as OrganizationIdentifier),
                uploadStateToken.Identifier.UploadKey,
                fileLocator,
                uploadChunk.Identifier.UploadChunkKey,
                chunkIndex,
                uploadContext.TotalChunks,
                uploadContext.SequentialState,
                uploadChunk.PositionFrom,
                uploadChunk.PositionTo,
                uploadContext.FileLength,
                inputStream,
                cancellationToken
            );

            uploadChunk.State = uploadContext.SequentialState;
            uploadChunk.Success = true;

            await UploadChunkStore.InsertAsync(uploadChunk);

            return uploadContext;
        }

        public async Task<FileModel> UploadCompleteAsync(
            UploadContextModel uploadContext,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var uploadStateToken = UploadTokenModel.Parse(uploadContext.UploadToken);

            var upload = await UploadStore.GetOneAsync(uploadStateToken.Identifier, new[]
            {
                new PopulationDirective
                {
                    Name = nameof(UploadModel.Chunks)
                }
            });

            var chunkStates = upload.Chunks?.Rows
                .OrderBy(c => c.ChunkIndex)
                .Select(c => new ChunkedStatusModel
                {
                    ChunkIndex = c.ChunkIndex,
                    UploadChunkKey = c.Identifier.UploadChunkKey,
                    State = c.State,
                    Success = true
                })
                .ToArray();

            var fileLocator = await FileStore.FileLocatorGetAsync(uploadStateToken.Identifier);

            var returnData = await BackendClient.CompleteChunkedUploadAsync(
                await LoadConfigurationAsync(uploadStateToken.Identifier as OrganizationIdentifier),
                uploadStateToken.Identifier.UploadKey,
                fileLocator,
                chunkStates
            );

            var fileModel = await FileStore.GetOneAsync(uploadStateToken.Identifier);
            await FileStore.UpdateAsync(fileModel);

            if (returnData != null)
                await FileStore.HashSetAsync(
                    fileModel.Identifier,
                    GetHash(returnData, "md5"),
                    GetHash(returnData, "sha1"),
                    GetHash(returnData, "sha256")
                );

            await FileStore.UploadingStatusSetAsync(fileModel.Identifier, false);

            await UploadStore.Cleanup(upload.Identifier);

            var evt = new FileContentsUploadCompleteEvent
            {
                FileIdentifier = fileModel.Identifier
            };
            evt.Populate(fileModel);

            await EventSender.SendAsync(evt);

            return fileModel;
        }

        public async Task<FileModel> UploadEntireFileAsync(
            FileModel fileModel, 
            Stream stream,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var context = await this.UploadBeginAsync(fileModel);
            context = await this.UploadChunkAsync(context, 0, fileModel.Length - 1, stream, 0);
            return await this.UploadCompleteAsync(context);
        }

        public async Task<bool> SetTagsAsync(
            FileIdentifier fileIdentifier,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var fileLocator = await FileStore.FileLocatorGetAsync(fileIdentifier);

            return await BackendClient.SetTagsAsync(
                await LoadConfigurationAsync(fileIdentifier as OrganizationIdentifier),
                fileLocator,
                tags,
                cancellationToken
            );
        }

        public async Task<Dictionary<string, string>> GetTagsAsync(
            FileIdentifier fileIdentifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var fileLocator = await FileStore.FileLocatorGetAsync(fileIdentifier);

            return await BackendClient.GetTagsAsync(
                await LoadConfigurationAsync(fileIdentifier as OrganizationIdentifier),
                fileLocator,
                cancellationToken
            );
        }

        public async Task<FileBackendConstants.OnlineStatus> GetOnlineStatusAsync(
            FileIdentifier fileIdentifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var fileLocator = await FileStore.FileLocatorGetAsync(fileIdentifier);

            return await BackendClient.GetOnlineStatusAsync(
                await LoadConfigurationAsync(fileIdentifier as OrganizationIdentifier),
                fileLocator,
                cancellationToken
            );
        }
        
        public async Task<bool> RequestOnlineAsync(
            FileIdentifier fileIdentifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var fileLocator = await FileStore.FileLocatorGetAsync(fileIdentifier);

            return await BackendClient.RequestOnlineAsync(
                await LoadConfigurationAsync(fileIdentifier as OrganizationIdentifier),
                fileLocator,
                cancellationToken
            );
        }

        private string GetHash(IDictionary<string, object> values, string key)
        {
            if (values.ContainsKey(key))
                return values[key] as string;
            else
                return null;
        }
    }
}
