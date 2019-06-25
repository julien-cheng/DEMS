namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Modules;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/batchnative")]
    public class BatchNativeController : ManagerControllerBase
    {
        private readonly FileService FileService;
        private readonly PathService PathService;
        private readonly APIConnection api;

        public BatchNativeController(FileService fileService, PathService pathService, APIConnection api)
        {
            FileService = fileService;
            PathService = pathService;
            this.api = api;
        }

        [HttpPost]
        [Description("Execute a set of operations")]
        public async Task Post(
            [FromForm] string payloadJSON,
            CancellationToken cancellationToken
        )
        {
            // don't write the extra json around our response, because our response is a file not json
            SuppressWrapper = true;

            var request = JsonConvert.DeserializeObject<BatchRequest>(payloadJSON);
            var setOperations = request.Operations.Select(o => o.Type).ToHashSet();

            // if all operations are in this set of types
            if (setOperations.All(o => new string[] {
                    typeof(DownloadZipFileRequest).Name,
                }.Any(s => s == o)))
            {
                var allFileIdentifiers = new List<FileIdentifier>();

                // add all FileIdentifiers in the request
                allFileIdentifiers.AddRange(request.Operations
                    .OfType<DownloadZipFileRequest>()
                    .Where(r => r.FileIdentifier != null)
                    .Select(d => d.FileIdentifier));


                // for each path in the request, load the recursive set of 
                var pathIdentifiers = request.Operations
                    .OfType<DownloadZipFileRequest>()
                    .Where(r => r.PathIdentifier != null)
                    .Select(d => d.PathIdentifier);

                var zipName = "File.zip";

                var firstPath = pathIdentifiers?.FirstOrDefault();
                var isEDiscoveryPath = EDiscoveryUtility.IsEDiscoveryPath(firstPath);
                var firstFile = allFileIdentifiers.Any()
                    ? await api.File.GetAsync(allFileIdentifiers.First())
                    : null;

                if (firstFile != null)
                { 
                    var packageName = firstFile.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE);
                    if (packageName != null)
                    {
                        zipName = PathService.MakeFilenameSafe($"{packageName}.zip");
                        isEDiscoveryPath = true;
                    }
                }

                if (pathIdentifiers.Any())
                {
                    // we're going to assume that all the paths are in the same folder. if not, this
                    // isn't going to work.
                    var state = await PathService.OpenFolder(
                        firstPath as FolderIdentifier,
                        isEDiscoveryPath,
                        cancellationToken
                    );


                    var packageIdentifier = EDiscoveryUtility.GetPackageIdentifier(firstPath);
                    if (packageIdentifier != null)
                    {
                        foreach (var pathIdentifier in pathIdentifiers)
                        {
                            var relativePath = pathIdentifier.RelativeTo(packageIdentifier);
                            allFileIdentifiers.AddRange(state.Folder.Files.Rows
                                .Where(f =>
                                {
                                    var filePath = f.MetaEDiscoveryPathIdentifierRead();
                                    if (filePath == null)
                                        return relativePath.IsRoot;
                                    else
                                        return filePath.IsChildOf(relativePath);
                                })
                                .Where(f => !f.Read<bool>(MetadataKeyConstants.HIDDEN))
                                .Select(f => f.Identifier)
                            );
                        }
                    }
                    else
                    {
                        foreach (var pathIdentifier in pathIdentifiers)
                            allFileIdentifiers.AddRange(
                                PathService.GetPathDescendants(state.Folder, pathIdentifier)
                                    .Select(f => f.Identifier)
                            );
                    }
                }

                await PathService.DownloadZip(zipName, allFileIdentifiers, Response, 
                    pathGenerator: f => isEDiscoveryPath
                        ? EDiscoveryUtility.MetaEDiscoveryPathIdentifierRead(f)
                        : APIModelExtensions.MetaPathIdentifierRead(f),
                    cancellationToken: cancellationToken);
            }
        }
    }
}