namespace Documents.Clients.Manager.Services
{
    using Common;
    using Documents.API.Client;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Modules;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Filters.Watermarks;
    using Documents.Queues.Messages;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class FileService : ServiceBase<ManagerFileModel, FileIdentifier>
    {
        private readonly APIConnection connection;
        public FileService(APIConnection connection) : base(connection)
        {
            this.connection = connection;
        }

        public async Task FileDownloadAsync(DownloadRequest request, HttpRequest httpRequest, HttpResponse httpResponse, CancellationToken cancellationToken = default(CancellationToken))
        {
            var fileModel = await Connection.File.GetAsync(request.FileIdentifier);
            var organization = await Connection.Organization.GetAsync(request.FileIdentifier as OrganizationIdentifier);

            var managerModel = ModelConvert(organization, fileModel,connection.UserTimeZone);
            
            long? from = 0;
            long? to = null;

            httpResponse.Headers[HeaderNames.AcceptRanges] = "bytes";

            if (httpRequest.Headers["Range"].Any())
            {
                var range = RangeHeaderValue.Parse(httpRequest.Headers["Range"].ToString()).Ranges.FirstOrDefault();
                if (range.From.HasValue)
                    from = range.From.Value;
                if (range.To.HasValue)
                    to = Math.Max(range.To.Value, 0);

                if (to == null)
                    to = fileModel.Length - 1;
            }

            await ExecuteDownload(request, httpResponse, fileModel, from, to, cancellationToken);
        }

        public async Task<ManagerFileModel> SaveFormData(SaveDataRequest saveDataRequest)
        {
            // First we're going to 'open' the file, and get all that metadata filled out.
            var fileModel = await Connection.File.GetAsync(saveDataRequest.FileIdentifier);
            var organization = await Connection.Organization.GetAsync(saveDataRequest.FileIdentifier as OrganizationIdentifier);

            SchemaForm.UpdateFormData(saveDataRequest.Data, fileModel);

            await Connection.File.PutAsync(fileModel);

            return ModelConvert(organization, fileModel, connection.UserTimeZone);
        }

        private async Task ExecuteDownload(
            DownloadRequest request,
            HttpResponse httpResponse,
            FileModel fileModel,
            long? from=0,
            long? to=0,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            // setup a handler to configure response headers
            void doHeaders(DownloadHeaderInformation headerInfo)
            {
                if (from != 0 || to != null)
                {
                    httpResponse.StatusCode = 206;
                    httpResponse.Headers.Add("Content-Range", new ContentRangeHeaderValue(
                        headerInfo.RangeFrom,
                        headerInfo.RangeTo,
                        headerInfo.RangeLength
                    ).ToString());
                }

                var contentDisposition = new ContentDispositionHeaderValue(request.Open ? "inline" : "attachment");
                contentDisposition.SetHttpFileName(headerInfo.FileName);

                httpResponse.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
                httpResponse.Headers[HeaderNames.Pragma] = "no-cache";
                httpResponse.Headers[HeaderNames.AcceptRanges] = "bytes";
            }

            if (WatermarkFilter.IsSupportedFileType(fileModel.Extension) && EDiscoveryUtility.IsUserEDiscovery(Connection.UserAccessIdentifiers))
            {
                // we're going to inject the Watermarker into the stream processing
                // so we don't use the standard FileDownloadAsync overload, instead we pass a stream callback
                await Connection.File.DownloadAsync(
                    fileModel.Identifier,
                    onDownloadHeaderInformation: doHeaders,
                    onStreamAvailable: async (stream, cancel) =>
                    {
                        // If we're downloading a child file, for instance in the case of a docx, we're generating a pdf.  
                        // when we generate that pdf, it won't inherit the metadata around eDiscovery.  We need to get that file info, so we can get a share package name.
                        FileIdentifier parentIdentifier;
                        FileModel parentFileModel = fileModel;

                        if ((parentIdentifier = fileModel.MetaChildOfRead()) != null)
                            parentFileModel = await Connection.File.GetAsync(parentIdentifier);
                        else
                            parentFileModel = fileModel;

                        // stream is a readstream of the file from the API server
                        // pass that to the Watermarker adn the HTTPResponse body stream as the output
                        // We need to get the path name for this file, which will give us what the discovery package it's in.
                        var packageName = parentFileModel.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE);
                        var watermarkUser = await Connection.User.GetAsync(Connection.UserIdentifier);

                        
                        // this code is redundant with PathService.DownloadZip
                        await WatermarkFilter.Execute(stream, 
                            httpResponse.Body, 
                            fileModel.Extension, 
                            $"Defense {packageName} {watermarkUser.EmailAddress}"
                        );
                    },

                    from: from,
                    to: to,

                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await Connection.File.DownloadAsync(
                    fileModel.Identifier,
                    onDownloadHeaderInformation: doHeaders,
                    onStreamAvailable: (stream, cancel) => stream.CopyToAsync(httpResponse.Body),
                    from: from,
                    to: to,
                    cancellationToken: cancellationToken
                );
            }
        }

        public async override Task<FileIdentifier> DeleteOneAsync(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            await Connection.File.DeleteAsync(identifier);

            var folder = await Connection.Folder.GetAsync(identifier as FolderIdentifier, new List<PopulationDirective>
            {
                new PopulationDirective(nameof(FolderModel.Files))
            });

            foreach (var file in folder.Files.Rows)
                if (file.MetaChildOfRead()?.Equals(identifier) ?? false)
                    await Connection.File.DeleteAsync(file.Identifier, cancellationToken: cancellationToken);

            return identifier;
        }


        public async Task ExtractFile(FileIdentifier fileIdentifier)
        {
            await Connection.Queue.EnqueueAsync("Archive", new ArchiveMessage(fileIdentifier));
        }

        public async override Task<ManagerFileModel> QueryOneAsync(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var file = await Connection.File.GetAsync(identifier, cancellationToken: cancellationToken);
            var organization = await Connection.Organization.GetAsync(identifier as OrganizationIdentifier);

            return ModelConvert(organization, file, Connection.UserTimeZone);
        }

        public async Task RenameAsync(
            FileIdentifier identifier,
            string newName,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var fileModel = await Connection.File.GetAsync(identifier, cancellationToken: cancellationToken);
            fileModel.Name = newName;
            await Connection.File.PutAsync(fileModel, cancellationToken: cancellationToken);
        }

        public ManagerFileModel ModelConvert(OrganizationModel organization, FileModel fileModel, string userTimeZone, List<IModule> activeModules = null, PathIdentifier virtualPathIdentifier = null, List<AttributeLocator> attributeLocators = null)
        {
            var shareStateIcon = EDiscoveryUtility.GetShareStateIcon(fileModel);
            var earrStateIcon = EArraignment.GetEArrainmentStateIcon(fileModel);

            var pathIdentifier = fileModel.MetaPathIdentifierRead();
            if(virtualPathIdentifier != null)
            {
                pathIdentifier = virtualPathIdentifier;
            }

            var managerFileModel = new ManagerFileModel
            {
                Identifier = fileModel.Identifier,
                Name = fileModel.Name,
                Length = fileModel.Length,
                LengthForHumans = fileModel.LengthForHumans,
                Created = fileModel.Created.ConvertToLocal(userTimeZone),
                Modified = fileModel.Modified.ConvertToLocal(userTimeZone),
                Views = ViewSetService.DetectFileViews(organization, fileModel),
                AllowedOperations = GetOperations(fileModel, pathIdentifier, activeModules, virtualPathIdentifier),
                PathIdentifier = pathIdentifier
            };

            var alternativeViews = fileModel.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);
            // this should probably be using the mediaservices somehow
            var thumbnail = alternativeViews
                ?.FirstOrDefault(v =>
                        v.SizeType == "Thumbnail"
                        || (v.SizeType == null
                            && v.ImageFormat == ImageFormatEnum.PNG
                            && v.Height == 100) // there was a period where SizeType was missing
                    );
            managerFileModel.PreviewImageIdentifier =  thumbnail?.FileIdentifier;

            // Now we need to build up the attributes on the file.
            // First we need to get the index from the folder.
            managerFileModel.Attributes = new Dictionary<string, object>();
            if (attributeLocators != null)
            {
                foreach (var attributeLocator in attributeLocators)
                {
                    if (attributeLocator.IsOnDetailView)
                    {
                        // This attribute locator points to a potential attribute that should exist on the file.
                        // figure out if this attribute is actually defined on the file.
                        var storageType = Type.GetType(attributeLocator.GetTypeNameFromStorageType());
                        var attributeValue = fileModel.Read(storageType, attributeLocator.Key);
                        if (attributeValue != null)
                        {
                            managerFileModel.Attributes.Add(attributeLocator.Label, attributeValue);
                        }
                    }
                }
            }

            // Now we need to add the hashes to our attributes as well.
            managerFileModel.Attributes.Add("MD5 Hash", ConvertFromBase64ToHex(fileModel.HashMD5));
            managerFileModel.Attributes.Add("SHA1 Hash", ConvertFromBase64ToHex(fileModel.HashSHA1));
            managerFileModel.Attributes.Add("SHA256 Hash", ConvertFromBase64ToHex(fileModel.HashSHA256));

            managerFileModel.DataModel = fileModel.Read<Dictionary<string, object>>(MetadataKeyConstants.SCHEMA_DEFINITION);

            foreach (var (key, value) in fileModel.FileMetadata["file"]) {
                if (key != "attribute.alternativeviews") {
                    managerFileModel.Attributes.Add(key, value);
                }
            }

            var defaultView = managerFileModel.Views.FirstOrDefault();

            managerFileModel.ViewerType = defaultView?.ViewerType ?? ManagerFileView.ViewerTypeEnum.Unknown;

            var icons = defaultView?.Icons.ToList() ?? new List<string>();
            icons.Add(shareStateIcon);
            icons.Add(earrStateIcon);

            managerFileModel.Icons = icons.Where(i => i != null).ToList();

            return managerFileModel;
        }

        private static string ConvertFromBase64ToHex(string base64Value)
        {
            if (base64Value == null)
                return null;
            else
                return BitConverter.ToString(Convert.FromBase64String(base64Value)).Replace("-", "");
        }

        private AllowedOperation[] GetOperations(
            FileModel fileModel,
            PathIdentifier pathIdentifier,
            List<IModule> activeModules,
            PathIdentifier virtualPathIdentifier = null
        )
        {
            var allowed = new List<AllowedOperation>
            {
                AllowedOperation.GetAllowedOperationDownload(fileModel.Identifier, false),
                AllowedOperation.GetAllowedOperationDownloadZip(fileModel.Identifier),
                AllowedOperation.GetAllowedOperationMove(fileModel.Identifier),
                AllowedOperation.GetAllowedOperationRename(fileModel.Identifier),
            };

            if (fileModel.PrivilegeCheck("delete", connection, throwOnFail: false))
                allowed.Add(AllowedOperation.GetAllowedOperationDelete(fileModel.Identifier));

            if (fileModel.Extension == "zip")
                allowed.Add(AllowedOperation.GetAllowedOperationExtractZip(fileModel.Identifier));

            // here we're going to process our modules in 2 phases.
            // some modules like eDiscovery are going to clean out all the allowed operations.  So they must run last.
            if(activeModules != null)
            {
                foreach (var module in activeModules)
                {
                    module.SetInitialAllowedOperations(fileModel, allowed, virtualPathIdentifier);
                }

                foreach (var module in activeModules)
                {
                    module.OverrideAllowedOperations(fileModel, allowed, virtualPathIdentifier);
                }
            }

            return allowed.ToArray();
        }

        public async Task<bool> RequestOnlineAsync(FileIdentifier identifier)
        {
            await Connection.File.RequestOnlineStatusAsync(identifier);

            return true;
        }
    }
}