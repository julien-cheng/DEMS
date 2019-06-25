namespace Documents.Clients.Manager.Services
{
    using Common;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common.PathStructure;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Clients.Manager.Modules;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Modules.ItemQueryHandlers;
    using Documents.Clients.Manager.Services.Models;
    using Documents.Filters.Watermarks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class PathService : ServiceBase<ManagerPathModel, PathIdentifier>
    {
        static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

        private readonly IAuditLogStore auditLogStore;
        private readonly ViewSetService viewSetService;
        private readonly FileService fileService;
        private readonly ModuleConfigurator moduleConfigurator;
        private readonly ManagerConfiguration ManagerConfiguration;

        public PathService(
            APIConnection connection,
            MetadataAuditLogStore auditLogStore,
            ViewSetService viewSetService,
            ModuleConfigurator moduleConfigurator,
            ManagerConfiguration managerConfiguration,
            FileService fileService
        ) : base(connection)
        {
            this.auditLogStore = auditLogStore;
            this.viewSetService = viewSetService;
            this.moduleConfigurator = moduleConfigurator;
            this.ManagerConfiguration = managerConfiguration;
            this.fileService = fileService;
        }

        public async Task<BatchOperationResponse> MoveIntoAsync(
            PathIdentifier pathIdentifier,
            PathIdentifier sourcePathIdentifier,
            FileIdentifier sourceFileIdentifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var state = await OpenFolder(pathIdentifier);
            long affectedCount = 0;

            if (sourceFileIdentifier != null)
                affectedCount = await MoveFileToPath(pathIdentifier, sourceFileIdentifier);

            if (sourcePathIdentifier != null)
                affectedCount = await MovePathToPath(state.Folder, pathIdentifier, sourcePathIdentifier);

            return new BatchOperationResponse
            {
                AffectedCount = affectedCount
            };
        }

        public async Task DownloadZip(
            string fileName,
            IEnumerable<FileIdentifier> fileIdentifiers,
            HttpResponse response,
            Func<FileModel, PathIdentifier> pathGenerator = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (pathGenerator == null)
                pathGenerator = APIModelExtensions.MetaPathIdentifierRead;

            response.StatusCode = 200;
            response.ContentType = "application/zip";
            var contentDisposition = new ContentDispositionHeaderValue("attachment");
            contentDisposition.SetHttpFileName(fileName);
            response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

            var isEDiscoveryUser = EDiscoveryUtility.IsUserEDiscovery(Connection.UserAccessIdentifiers);
            OrganizationModel organization = null;

            using (var archive = new ZipArchive(response.Body, ZipArchiveMode.Create))
            {
                foreach (var fileIdentifier in fileIdentifiers)
                {

                    if (organization == null || organization.Identifier.OrganizationKey != fileIdentifier.OrganizationKey)
                        organization = await Connection.Organization.GetAsync(fileIdentifier as OrganizationIdentifier);

                    var fileModel = await Connection.File.GetAsync(fileIdentifier);
                    var views = ViewSetService.DetectFileViews(organization, fileModel);

                    var newFileExtension = null as string;
                    var contentsFileIdentifier = fileIdentifier;

                    if (views.Any())
                    {
                        var first = views.First();
                        switch (first.ViewerType)
                        {
                            case ManagerFileView.ViewerTypeEnum.Video:
                                var mediaSet = await viewSetService.LoadSet<MediaSet>(fileIdentifier);

                                var mp4 = mediaSet.Sources.FirstOrDefault(s => s.Type == "video/mp4");
                                if (mp4 != null)
                                {
                                    newFileExtension = "mp4";
                                    contentsFileIdentifier = mp4.FileIdentifier;
                                }

                                break;
                            case ManagerFileView.ViewerTypeEnum.Document:

                                if (fileModel.Extension == "docx")
                                {
                                    var documentSet = await viewSetService.LoadSet<DocumentSet>(fileIdentifier);

                                    var pdf = documentSet.FileIdentifier;
                                    if (pdf != null)
                                    {
                                        newFileExtension = "pdf";
                                        contentsFileIdentifier = pdf;
                                    }
                                }
                                break;
                        }
                    }


                    var path = pathGenerator(fileModel);

                    string name = fileModel.Name;
                    if (newFileExtension != null)
                        name = $"{Path.GetFileNameWithoutExtension(name)}.{newFileExtension}";
                    else
                        newFileExtension = fileModel.Extension;

                    name = MakeFilenameSafe(name);

                    var filename = path != null
                        ? Path.Combine(path.PathKey, name)
                        : name;

                    var entry = archive.CreateEntry(filename);

                    using (var fileStream = entry.Open())
                    {
                        await Connection.File.DownloadAsync(
                            contentsFileIdentifier,
                            onStreamAvailable: async (stream, cancel) =>
                            {
                                if (isEDiscoveryUser && WatermarkFilter.IsSupportedFileType(newFileExtension))
                                {
                                    var packageName = fileModel.Read<string>(
                                        MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE);

                                    var watermarkUser = await Connection.User.GetAsync(Connection.UserIdentifier);

                                    // this code is redundant with FileService.ExecuteDownload
                                    await WatermarkFilter.Execute(stream,
                                        fileStream,
                                        newFileExtension,
                                        $"Defense {packageName} {watermarkUser.EmailAddress}"
                                    );
                                }
                                else
                                    await stream.CopyToAsync(fileStream);
                            },

                            cancellationToken: cancellationToken
                        );
                    }
                }
            }
        }

        public static string MakeFilenameSafe(string name)
        {
            // Builds a string out of valid chars and an _ for invalid ones
            return new string(name
                .Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch)
                .ToArray()
            );
        }

        private async Task<long> MoveFileToPath(
            PathIdentifier destinationPathIdentifier,
            FileIdentifier fileIdentifier
        )
        {
            var file = await Connection.File.GetAsync(fileIdentifier);
            file.MetaPathIdentifierWrite(destinationPathIdentifier);
            await Connection.File.PutAsync(file);

            return 1;
        }

        public IEnumerable<FileModel> GetPathDescendants(FolderModel folder, PathIdentifier pathIdentifier)
        {
            return folder
                .Files.Rows
                .Where(f => f?.MetaPathIdentifierRead().IsChildOf(pathIdentifier) ?? false);
        }

        private async Task<long> MovePathToPath(
            FolderModel folder,
            PathIdentifier destinationPathIdentifier,
            PathIdentifier sourcePathIdentifier
        )
        {
            // if we're moving
            // z of x/y/z to a/b/c
            // then pathDestination will be a/b/c/z
            var pathDestination = destinationPathIdentifier.CreateChild(sourcePathIdentifier.LeafName);

            long affectedCount = 0;

            // find files in contained by our source path, or a child of it
            var descendants = GetPathDescendants(folder, sourcePathIdentifier);

            foreach (var file in descendants)
            {
                var path = file.MetaPathIdentifierRead();

                // todo: move this logic to PathProcessor
                // replace the old key with the new key
                path.PathKey = Regex.Replace
                (
                    path?.PathKey,
                    $"^{Regex.Escape(sourcePathIdentifier.PathKey)}",
                    pathDestination.PathKey
                );

                file.MetaPathIdentifierWrite(path);
                await Connection.File.PutAsync(file);

                affectedCount++;
            }

            // update the folder's path reservations
            var paths = folder.Read<List<string>>("_paths");
            if (paths != null)
            {
                var pathsAffected = 0;
                for (int i = 0; i < paths.Count; i++)
                {
                    if (paths[i].StartsWith(sourcePathIdentifier.PathKey))
                    {
                        pathsAffected++;
                        paths[i] = paths[i].Replace(
                            sourcePathIdentifier.PathKey,
                            pathDestination.PathKey
                        );
                    }
                }
                if (pathsAffected > 0)
                {
                    folder.Write("_paths", paths);
                    await Connection.Folder.PutAsync(folder);
                }
            }
            return affectedCount;
        }

        public async Task<IEnumerable<ManagerPathModel>> SuggestAsync(
            PathIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            // we're querying the folder at this path location
            // presumably the one we are about to create a new folder in.
            // so we check the template structure (state.Structure) 
            // and compare it to our current tree.

            var suggestedFolder = await Connection.Folder.GetAsync(
                new FolderIdentifier(identifier as OrganizationIdentifier, ":suggestedpaths")
            );

            if (suggestedFolder != null)
            {

                // take note that we're NOT using the suggestedPath folder identifier
                // below. we're using the local path identifier
                var suggestedProcessor = new PathProcessor(identifier as FolderIdentifier);
                suggestedProcessor.Read(suggestedFolder, skipFiles: true);

                var structurePath = suggestedProcessor[identifier];

                // not just pulling the folder, because OpenFolder may add paths for a variety of reasons
                var stateThisFolder = await OpenFolder(identifier as FolderIdentifier);
                var thisPath = stateThisFolder.Paths[identifier];

                // suggestedPaths.
                // reject any subpaths that already exist
                return structurePath.Paths?
                    .Where(s => thisPath.Paths == null || !thisPath.Paths.Any(p => p.FullPath == s.FullPath))
                    ?? new ManagerPathModel[0];
            }
            else
                return new ManagerPathModel[0];
        }

        public async Task<ItemQueryResponse> ItemQueryAsync(
            PathIdentifier identifier,
            bool isEdiscoveryUser = false,
            int pageIndex = 0,
            int pageSize = 500,
            string sortField = "Name",
            bool sortAscending = true,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var folderModel = await Connection.Folder.GetOrThrowAsync(identifier);

            var activeModules = this.moduleConfigurator.GetActiveModules(folderModel);

            var handler = this.moduleConfigurator.GetActiveHandler(identifier, activeModules, this, auditLogStore, this.ManagerConfiguration, this.fileService);

            return await handler.HandleItemQuery(identifier, activeModules, true, pageIndex, pageSize, sortField, sortAscending, cancellationToken);
        }

        public override Task<ManagerPathModel> QueryOneAsync(PathIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(new ManagerPathModel
            {
                Identifier = identifier,
                FullPath = identifier.FullName,
                Name = identifier.LeafName
            });
        }

        public async Task<PathServiceState> OpenFolder(FolderIdentifier identifier, bool skipPathParse = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var folder = await Connection.Folder.GetAsync(identifier, new List<PopulationDirective>
            {
                new PopulationDirective
                {
                    Name = nameof(FolderModel.Files)
                    // and paging bits
                }
            });

            if (folder != null)
            {
                var state = new PathServiceState()
                {
                    Folder = folder,
                    Paths = new PathProcessor(identifier)
                };

                // add and filter the path reservations from metadata
                if (!skipPathParse)
                    state.Paths.Read(state.Folder);

                return state;
            }
            else
                throw new Exception("Folder does not exist");
        }

        public async Task<ManagerPathModel> CreateChildAsync(
            PathIdentifier parentPathIdentifier,
            string name,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var state = await OpenFolder(parentPathIdentifier as FolderIdentifier, cancellationToken: cancellationToken);
            var newPathIdentifier = parentPathIdentifier.CreateChild(name?.Trim());
            state.Paths.Add(newPathIdentifier);
            state.Paths.Write(state.Folder);
            await Connection.Folder.PutAsync(state.Folder);

            return (new ManagerPathModel
            {
                Identifier = newPathIdentifier,
                FullPath = newPathIdentifier.FullName,
                Name = newPathIdentifier.LeafName
            });

        }

        // todo: this is really create child from parent identifier.
        // should probably not be an upsert implementation.
        public override Task<ManagerPathModel> UpsertOneAsync(
            ManagerPathModel model,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            throw new NotImplementedException();
        }

        public async override Task<PathIdentifier> DeleteOneAsync(PathIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await OpenFolder(identifier, cancellationToken: cancellationToken);
            await state.Paths.Delete(identifier, async (doomedPath) =>
            {
            // find any files under the path to be deleted
            var doomedFiles = state.Folder?.Files.Rows.Where(f =>
            {
                var filePathIdentifier = f.MetaPathIdentifierRead();
                return (filePathIdentifier.Equals(doomedPath))
                || (filePathIdentifier.IsChildOf(doomedPath));
            }).ToList();

            // Before we go deleting anything we need to see if any of these files have been shared. 
            // If they have been shared we're not going to allow them to be deleted.
            doomedFiles = doomedFiles.Where(f => EDiscoveryUtility.GetCurrentShareState(f) != EDiscoveryShareState.Published).ToList();

                foreach (var doomedFile in doomedFiles)
                {
                    await Connection.File.DeleteAsync(doomedFile.Identifier, cancellationToken: cancellationToken);

                    var doomedChildren = state.Folder.Files.Rows
                        .Where(f => doomedFile.Identifier.Equals(f.MetaChildOfRead()))
                        .ToList();

                    foreach (var childOfDoomed in doomedChildren)
                        await Connection.File.DeleteAsync(childOfDoomed.Identifier, cancellationToken: cancellationToken);
                }
            });

            // if we filtered any out above, write the new list back to the folder
            if (state.Paths.IsDirty)
            {
                state.Paths.Write(state.Folder);
                await Connection.Folder.PutAsync(state.Folder);
            }

            return identifier;
        }

        public async Task RenameAsync(PathIdentifier identifier, string newName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await OpenFolder(identifier, cancellationToken: cancellationToken);

            var newPathIdentifier = identifier.ParentPathIdentifier.CreateChild(newName);

            // find any files under the path to be deleted
            var fileList = state.Folder?.Files?.Rows.Where(f =>
                {
                    var pathIdentifier = f.MetaPathIdentifierRead();
                    return (pathIdentifier == null && identifier.IsRoot)
                        || (pathIdentifier != null && pathIdentifier.IsChildOf(identifier));
                })
                .ToList();

            // loop the affected files
            foreach (var file in fileList)
            {
                var path = file.MetaPathIdentifierRead();

                if (path != null)
                {
                    path.PathKey = Regex.Replace
                    (
                        path?.PathKey,
                        $"^{Regex.Escape(identifier.PathKey)}",
                        newPathIdentifier.PathKey
                    );

                    file.MetaPathIdentifierWrite(path);

                    await Connection.File.PutAsync(file);
                }
            }

            var pathlist = state.Paths.All
                .Select(p => p.FullName)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            var oldPath = identifier.FullName;

            foreach (var definedPath in pathlist.ToList())
                if (definedPath == oldPath
                    || (definedPath.Length > oldPath.Length
                        && definedPath.StartsWith(oldPath + '/')))
                {
                    pathlist.Remove(definedPath);
                    pathlist.Add(definedPath.Replace(oldPath, newPathIdentifier.FullName));
                }

            if (!pathlist.Any())
                pathlist.Add(null);

            var folder = await Connection.Folder.GetAsync(identifier as FolderIdentifier);
            folder.Write("_paths", pathlist);
            await Connection.Folder.PutAsync(folder);
        }
    }
}