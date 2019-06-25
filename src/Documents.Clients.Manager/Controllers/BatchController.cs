namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/batch")]
    public class BatchController : ManagerControllerBase
    {
        private readonly FileService FileService;
        private readonly PathService PathService;
        private readonly TranscriptService TranscriptService;
        private readonly MediaService MediaService;
        private readonly ClipService ClipService;
        private readonly FolderService FolderService;
        private readonly IOptions<ManagerConfiguration> ManagerConfiguration;
        private readonly ModuleConfigurator ModuleConfigurator;

        public BatchController(
            IOptions<ManagerConfiguration> managerConfiguration, 
            FileService fileService, 
            PathService pathService, 
            ModuleConfigurator moduleConfigurator,
            TranscriptService transcriptService,
            MediaService mediaService,
            ClipService clipService,
            FolderService folderService)
        {
            this.FileService = fileService;
            this.PathService = pathService;
            this.ManagerConfiguration = managerConfiguration;
            this.ModuleConfigurator = moduleConfigurator;
            this.TranscriptService = transcriptService;
            this.MediaService = mediaService;
            this.ClipService = clipService;
            this.FolderService = folderService;
        }

        [HttpPost]
        [Description("Execute a set of operations")]
        public async Task<BatchResponse> Post(
            [FromBody] BatchRequest request,
            CancellationToken cancellationToken
        )
        {
            var responses = new List<APIResponse>();

            // We don't need to worry about whether the module is active or not here.  
            // Because only batch requests are going to come back if the module is active.
            List<IModule> allModules = this.ModuleConfigurator.GetAllModules();

            foreach (var operation in request.Operations)
            {
                foreach (var module in allModules)
                {
                    if (module.HasHandlerForBatchOperation(operation))
                    {
                        responses.Add(await APIExecuteAsync(() => module.HandleBatchOperation(operation)));
                    }
                }

                if (operation is ExtractRequest extract)
                {
                    responses.Add(
                        await APIExecuteAsync(async () =>
                            await FileService.ExtractFile(extract.FileIdentifier)
                        )
                    );
                }

                if (operation is MoveIntoRequest moveIntoRequest)
                {
                     
                    responses.Add(
                        await APIExecuteAsync(async () =>
                            await PathService.MoveIntoAsync(
                                moveIntoRequest.TargetPathIdentifier,
                                moveIntoRequest.SourcePathIdentifier,
                                moveIntoRequest.SourceFileIdentifier
                            )
                        )
                    );
                }

                if (operation is RenameRequest renameRequest)
                {
                    responses.Add(
                        await APIExecuteAsync(async () =>
                        {
                            if (renameRequest.FileIdentifier != null)
                                await FileService.RenameAsync(renameRequest.FileIdentifier, renameRequest.NewName);
                            if (renameRequest.PathIdentifier != null)
                                await PathService.RenameAsync(renameRequest.PathIdentifier, renameRequest.NewName);

                            return new BatchOperationResponse
                            {
                                AffectedCount = 1,
                                SuggestedAction = BatchOperationResponse.Actions.ReloadFolder
                            };

                        })
                    );

                }

                if (operation is DownloadRequest downloadRequest)
                {
                    await FileService.FileDownloadAsync(downloadRequest, Request, Response, cancellationToken);

                    return null;
                }

                if (operation is TranscriptionRequest transcriptionRequest)
                {
                    await TranscriptService.RequestTranscriptAsync(transcriptionRequest);

                    return null;
                }

                if (operation is DeleteRequest deleteRequest)
                {
                    responses.Add(
                        await APIExecuteAsync(async () =>
                        {
                            if (deleteRequest.FileIdentifier != null)
                                await FileService.DeleteOneAsync(deleteRequest.FileIdentifier);
                            if (deleteRequest.PathIdentifier != null)
                                await PathService.DeleteOneAsync(deleteRequest.PathIdentifier);

                            return new BatchOperationResponse
                            {
                                AffectedCount = 1,
                                SuggestedAction = BatchOperationResponse.Actions.ReloadPath
                            };
                        })
                    );
                }

                if (operation is WatermarkVideoRequest watermark)
                    await MediaService.WatermarkVideoAsync(watermark);

                if (operation is ExportClipRequest exportClip)
                    await ClipService.ExportClipAsync(exportClip);
                if (operation is ExportFrameRequest exportFrame)
                    await ClipService.ExportFrameAsync(exportFrame);

                if (operation is RequestOnlineRequest onlineRequest)
                {
                    var pathState = await PathService.OpenFolder(onlineRequest.FolderIdentifier);
                    foreach (var file in pathState.Folder.Files.Rows)
                        await FileService.RequestOnlineAsync(file.Identifier);
                }
            }

            return new BatchResponse
            {
                OperationResponses = responses
            };
        }
    }
}