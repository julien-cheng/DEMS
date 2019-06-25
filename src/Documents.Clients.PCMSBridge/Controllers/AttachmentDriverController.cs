namespace Documents.Clients.PCMSBridge.Controllers
{
    using API.Client;
    using API.Common.Models;
    using Clients.PCMSBridge.Models;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models.MetadataModels;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Encodings.Web;
    using System.Threading;
    using System.Threading.Tasks;

    public class AttachmentDriverController : Controller
    {
        private string userState = null;
        private readonly ILogger<AttachmentDriverController> Logger;
        private readonly PCMSBridgeConfiguration Configuration;

        public AttachmentDriverController(
            ILogger<AttachmentDriverController> logger,
            PCMSBridgeConfiguration configuration
        )
        {
            Logger = logger;
            Configuration = configuration;
        }

        private void CheckAuthentication(APIRequest request)
        {
            // We are slaving the Documents system to the PCMS Accounts,
            // and allowing impersonation, therefore there's no chance
            // for authentication failures.
        }

        private async Task<Connection> ConnectionCreate(APIRequest request)
        {
            try
            {
                Logger.LogInformation($"Connection Create {JsonConvert.SerializeObject(request?.Context)}");
                Logger.LogDebug(request.Context.CountyState);

                var config = CountyState.Read(request.Context.CountyState);

                var connection = new Connection(new Uri(Configuration.APIUrl))
                {
                    Logger = Logger
                };
                bool needToken = true;

                if (request.Context.UserState != null)
                {
                    Logger.LogDebug($"Reusing existing token");
                    connection.Token = request.Context.UserState;

                    needToken = !(await connection.HealthGetAsync());
                    Logger.LogDebug($"NeedNewToken: {needToken}");
                }

                if (needToken)
                {
                    var tokenResponse = await connection.User.AuthenticateAsync(new TokenRequestModel
                    {
                        Identifier = config.UserIdentifier,
                        Password = config.UserSecret
                    });

                    var userIdentifier = new UserIdentifier
                    {
                        OrganizationKey = config.UserIdentifier.OrganizationKey,
                        UserKey = request.Context.EmailAddress
                    };

                    await connection.ConcurrencyRetryBlock(async () =>
                    {

                        var user = await connection.User.GetAsync(userIdentifier);
                        if (user == null)
                        {
                            user = new UserModel(userIdentifier)
                            {
                                EmailAddress = request.Context.EmailAddress
                            };

                            user = await connection.User.PutAsync(user);
                        }

                        var identifiers = user.UserAccessIdentifiers?.ToList() ?? new List<string>();

                        bool dirty = false;

                        dirty = EnsureExists(identifiers, $"o:{user.Identifier.OrganizationKey}") || dirty;
                        dirty = EnsureExists(identifiers, $"x:eDiscovery") || dirty;

                        if (dirty)
                            await connection.User.AccessIdentifiersPutAsync(user.Identifier, identifiers);
                    });

                    tokenResponse = await connection.User.ImpersonateAsync(userIdentifier);

                    Logger.LogDebug($"Acquired new token: {connection.Token}");
                }

                userState = connection.Token;

                return connection;
            }
            catch (Exception e)
            {
                Logger.LogError(0, e, "Failed to login");
                throw;
            }
        }

        private static bool EnsureExists(List<string> list, string element)
        {
            if (list.Contains(element))
                return false;

            list.Add(element);
            return true;
        }

        [Route("healthcheck")]
        public string HealthCheck()
        {
            return "OK";
        }

        [Route("urls")]
        [HttpPost]
        public async Task<PCMSAPIResponse<URLs>> GetURLs(
            [FromBody]APIRequest<URLRequest> request
        )
        {
            CheckAuthentication(request);
            var connection = await ConnectionCreate(request);
            var token = connection.Token;

            var folderIdentifier = new FolderIdentifier(connection.UserIdentifier.OrganizationKey, $"Defendant:{request.Context.DefendantID}");
            var folder = await connection.Folder.GetAsync(folderIdentifier);
            if (folder == null)
                folder = await connection.Folder.PutAsync(new FolderModel(folderIdentifier));

            string autoDownloadKeys = null;
            if (request.Parameters?.AutoDownloadUniques?.Any() ?? false)
                autoDownloadKeys = string.Join("||", request.Parameters.AutoDownloadUniques);

            token = UrlEncoder.Default.Encode(token);

            var urlFormat = Configuration.FinalURL;

            if (request.Parameters?.DeepLink != null)
                urlFormat = Configuration.FinalURLDeepLink;

            var redirect = 
                string.Format(
                    urlFormat, 
                    request.Context.DefendantID, 
                    UrlEncoder.Default.Encode(autoDownloadKeys ?? string.Empty), 
                    request.Context.CountyID,
                    UrlEncoder.Default.Encode(request.Parameters?.DeepLink ?? string.Empty)
                );

            return new PCMSAPIResponse<URLs>
            {
                Response = new URLs
                {
                    IFrame = BuildHandoffURL(
                        redirect,
                        token),
                    GlobalSearch = BuildHandoffURL(
                        string.Format(Configuration.GlobalSearchURL, request.Context.CountyID),
                        token),
                }
            };
        }

        private string BuildHandoffURL(string url, string token)
        {
            return string.Format(Configuration.HandoffURL, UrlEncoder.Default.Encode(url), token);
        }


        [Route("attachments/deleteallowed")]
        public async Task<PCMSAPIResponse<bool>> EDiscoveryStatisticsAsync([FromBody]APIRequest request)
        {
            CheckAuthentication(request);
            var connection = await ConnectionCreate(request);
            var config = CountyState.Read(request.Context.CountyState);

            var folderKey = $"Defendant:{request.Context.DefendantID}";
            var folderIdentifier = new FolderIdentifier(config.UserIdentifier.OrganizationKey, folderKey);

            var stats = new EDiscoveryStatisticsResponse();

            // Setup our state so we have everything we need. 
            var folder = await connection.Folder.GetAsync(folderIdentifier, new List<PopulationDirective>
            {
                new PopulationDirective
                {
                    Name = nameof(FolderModel.Files)
                }
            });

            if (folder?.Files?.Rows != null)
                foreach (var file in folder.Files.Rows.ToList())
                {
                    switch (GetCurrentShareState(file))
                    {
                        case EDiscoveryShareState.NotShared:
                            // No Op here
                            break;
                        case EDiscoveryShareState.Staged:
                            stats.FilesStaged++;
                            break;
                        case EDiscoveryShareState.Published:
                            stats.FilesPublished++;
                            break;
                        default:
                            break;
                    }
                }

            if (folder != null)
            {
                // Really the thing stored at this metadata key is a EdiscoveryRecipient, but I'm hoping we can get away with just bringing it back out as an object, because we 
                // only need the count off of it. 
                stats.RecipientCount = folder.Read<List<object>>(MetadataKeyConstants.E_DISCOVERY_RECIPIENT_LIST, defaultValue: new List<object>()).Count();

                stats.IsEDiscoveryActive = folder.Read<bool>(MetadataKeyConstants.E_DISCOVERY_ACTIVE_METAKEY);
            }

            bool allowed = stats.FilesPublished == 0;

            return new PCMSAPIResponse<bool>
            {
                Response = allowed
            };
        }

        public enum EDiscoveryShareState
        {
            NotShared = 0,
            Staged = 1,
            Published = 2,
        }

        public static EDiscoveryShareState GetCurrentShareState(FileModel file)
        {
            var shareState = file.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY);
            if (shareState == null)
            {
                return EDiscoveryShareState.NotShared;
            }
            return shareState != null ? (EDiscoveryShareState)Enum.Parse(typeof(EDiscoveryShareState), shareState) : EDiscoveryShareState.NotShared;
        }

        [Route("attachments/moveFiles")]
        public async Task<PCMSAPIResponse<bool>> MoveAttachments(
            [FromBody]APIRequest<int> request
        )
        {
            CheckAuthentication(request);
            var connection = await ConnectionCreate(request);
            var folderKey = $"Defendant:{request.Context.DefendantID}";
            var folderKeyDestination = $"Defendant:{request.Parameters}";

            var config = CountyState.Read(request.Context.CountyState);

            // ensure everything exists before we try this
            var sourceFolderIdentifier = new FolderIdentifier(config.UserIdentifier.OrganizationKey, folderKey);
            var destinationFolderIdentifier = new FolderIdentifier(config.UserIdentifier.OrganizationKey, folderKeyDestination);

            var folder = await connection.Folder.GetAsync(sourceFolderIdentifier)
                    ?? await connection.Folder.PutAsync(new FolderModel(sourceFolderIdentifier));
            var folderDestination = await connection.Folder.GetAsync(destinationFolderIdentifier)
                    ?? await connection.Folder.PutAsync(new FolderModel(destinationFolderIdentifier));

            folder = await connection.Folder.GetAsync(sourceFolderIdentifier, new List<PopulationDirective>
            {
                new PopulationDirective(nameof(FolderModel.Files))
            });

            if (folder != null)
            {
                foreach (var file in folder.Files.Rows)
                {
                    await connection.ConcurrencyRetryBlock(async () =>
                    {
                        var updateFile = await connection.File.GetAsync(file.Identifier);
                        var views = updateFile.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);
                        if (views != null)
                        {
                            foreach (var view in views)
                            {
                                if (view.FileIdentifier.FolderKey == folder.Identifier.FolderKey)
                                    view.FileIdentifier.FolderKey = folderDestination.Identifier.FolderKey;
                            }
                            updateFile.Write(MetadataKeyConstants.ALTERNATIVE_VIEWS, views);
                        }

                        var childOf = updateFile.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF);
                        if (childOf != null)
                        {
                            if (childOf.FolderKey == childOf.FolderKey)
                                childOf.FolderKey = childOf.FolderKey;
                            updateFile.Write(MetadataKeyConstants.CHILDOF, childOf);
                        }

                        if (views != null || childOf != null)
                            await connection.File.PutAsync(updateFile);
                    });

                    await connection.File.MoveAsync(
                        new FileIdentifier(destinationFolderIdentifier, file.Identifier.FileKey),
                        file.Identifier
                    );
                }
            }

            return new PCMSAPIResponse<bool>
            {
                Response = true
            };
        }


        [Route("attachments/byDefendant")]
        [HttpPost]
        public PCMSAPIResponse<List<AttachmentFolder>> GetAttachments(
            [FromBody]APIRequest request
        )
        {
            CheckAuthentication(request);

            return new PCMSAPIResponse<List<AttachmentFolder>>
            {
                Response = new List<AttachmentFolder>(),
                State = new Models.PCMSAPIResponse.StateFields
                {
                    UserState = userState
                }
            };
        }

        private DateTime GetFileCreatedDate(FileModel fileModel)
        {
            try
            {
                var metaCreated = fileModel.Read<string>("created");
                if (metaCreated != null)
                    return DateTime.Parse(metaCreated);
            }
            catch (Exception) { }

            return fileModel.Created;
        }

        private static string FileIdentifierToUnique(FileIdentifier identifier)
        {
            return JsonConvert.SerializeObject(identifier);
        }

        private static FileIdentifier UniqueToFileIdentifier(string unique)
        {
            return JsonConvert.DeserializeObject<FileIdentifier>(unique);
        }

        [Route("attachments/byUniques")]
        [HttpPost]
        public async Task<PCMSAPIResponse<List<AttachmentFile>>> GetAttachments([FromBody]APIRequest<string[]> request,
            CancellationToken cancellationToken)
        {
            CheckAuthentication(request);
            var connection = await ConnectionCreate(request);

            string[] requestedAttachmentUniques = request.Parameters;

            var attachments = new List<AttachmentFile>();

            foreach (var unique in requestedAttachmentUniques)
            {
                var file = await connection.File.GetAsync(UniqueToFileIdentifier(unique), cancellationToken: cancellationToken);

                attachments.Add(new AttachmentFile
                {
                    Unique = FileIdentifierToUnique(file.Identifier),
                    Name = file.Name,
                    Description = file.Read<string>("Description"),
                    PathUnique = null,
                    Created = file.Created,
                    DefendantID = file.Read<int>("defendantid")
                });
            }

            return new PCMSAPIResponse<List<AttachmentFile>>
            {
                Response = attachments
            };
        }

        [Route("paths")]
        [HttpPost]
        public async Task<PCMSAPIResponse<Dictionary<string, string>>> GetAttachmentPaths(
            [FromBody]APIRequest request
        )
        {
            CheckAuthentication(request);            
            var connection = await ConnectionCreate(request);            
            var folder = await connection.Folder.GetAsync(new FolderIdentifier(connection.UserIdentifier.OrganizationKey, ":suggestedpaths"));
            var paths = folder.Read<List<string>>("_paths");
            var toReturn = paths.ToDictionary(x => x, x => x);
            return new PCMSAPIResponse<Dictionary<string, string>>() { Response = toReturn};
        }

        [Route("attachment/download")]
        [HttpPost]
        public async Task<PCMSAPIResponse<AttachmentContents>> DownloadAttachment([FromBody]APIRequest<string> request)
        {
            CheckAuthentication(request);

            var unique = request.Parameters;
            var fileIdentifier = UniqueToFileIdentifier(unique);

            var connection = await ConnectionCreate(request);
            var fileModel = await connection.File.GetAsync(fileIdentifier);

            using (var ms = new MemoryStream())
            {
                await connection.File.DownloadAsync(fileIdentifier, ms);
                var contents = new AttachmentContents
                {
                    ContentType = fileModel.MimeType,
                    FileName = fileModel.Name,
                    ContentsBase64 = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length)
                };

                return new PCMSAPIResponse<AttachmentContents>()
                {
                    Response = contents
                };
            }
        }

        [Route("attachment/delete")]
        [HttpPost]
        public PCMSAPIResponse DeleteAttachment([FromBody]APIRequest<string> request)
        {
            CheckAuthentication(request);
            return new PCMSAPIResponse();
        }

        [Route("attachment/move")]
        [HttpPost]
        public PCMSAPIResponse MoveAttachment([FromBody]APIRequest<MoveRequestModel> request)
        {
            CheckAuthentication(request);
            return null;
        }

        private OrganizationIdentifier Organization(APIRequest request)
            => new OrganizationIdentifier($"PCMS:{request.Context.CountyID}");

        [Route("attachment/upload")]
        [HttpPost]
        public async Task<PCMSAPIResponse<AttachmentFile>> UploadAttachment(
            [FromBody]APIRequest<UploadRequestModel> request,
            CancellationToken cancellationToken
        )
        {
            CheckAuthentication(request);

            var upload = request.Parameters;
            var connection = await ConnectionCreate(request);

            using (var ms = new MemoryStream(Convert.FromBase64String(upload.ContentsBase64)))
            {
                var folderKey = $"Defendant:{request.Context.DefendantID}";
                var folderModel = await connection.Folder.PutAsync(
                    new FolderModel(
                        new FolderIdentifier(
                            Organization(request), 
                            folderKey
                        )
                    )
                );

                var fileModel = new FileModel(new FileIdentifier(folderModel.Identifier, null))
                {
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                    Length = ms.Length,
                    Name = request.Parameters.Name,
                    MimeType = request.Parameters.IsGenerated
                            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                            : "application/octet-stream",
                    FileMetadata = new Dictionary<string, IDictionary<string, string>>()
                };
                fileModel.Write("defendantid", request.Context.DefendantID);
                fileModel.Write("description", upload.Description);
                if (!string.IsNullOrEmpty(upload.PathUnique))
                {
                    fileModel.Write("_path", upload.PathUnique);
                }

                fileModel = await connection.File.UploadAsync(fileModel, ms, cancellationToken: cancellationToken);

                return new PCMSAPIResponse<AttachmentFile>
                {
                    Response = new AttachmentFile
                    {
                        DefendantID = request.Context.DefendantID.Value,
                        Created = DateTime.UtcNow,
                        Description = request.Parameters.Description,
                        IsGenerated = request.Parameters.IsGenerated,
                        Name = request.Parameters.Name,
                        Unique = FileIdentifierToUnique(fileModel.Identifier)
                    }
                };
            }
        }
        
        private static bool IsMultipartContentType(string contentType)
        {
            return
                !string.IsNullOrEmpty(contentType) &&
                contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetBoundary(string contentType)
        {
            var elements = contentType.Split(' ');
            var element = elements.Where(entry => entry.StartsWith("boundary=")).First();
            var boundary = element.Substring("boundary=".Length);
            // Remove quotes
            if (boundary.Length >= 2 && boundary[0] == '"' &&
                boundary[boundary.Length - 1] == '"')
            {
                boundary = boundary.Substring(1, boundary.Length - 2);
            }
            return boundary;
        }

        private static string GetFileName(string contentDisposition)
        {
            return contentDisposition
                .Split(';')
                .SingleOrDefault(part => part.Contains("filename"))
                .Split('=')
                .Last()
                .Trim('"');
        }
        
        [Route("credentials/set")]
        [HttpPost]
        public PCMSAPIResponse SetCredentials([FromBody]APIRequest<string> request)
        {
            return new PCMSAPIResponse
            {
                State = new PCMSAPIResponse.StateFields
                {
                    UserState = null
                }
            };
        }

        private class StreamingFileResult : ActionResult
        {
            private Func<Stream, Action<string, string, long, bool>, Task> Content;

            public StreamingFileResult(Func<Stream, Action<string, string, long, bool>, Task> content)
            {
                this.Content = content ?? throw new ArgumentNullException("content");
            }

            public async override Task ExecuteResultAsync(ActionContext context)
            {
                await Content(context.HttpContext.Response.Body, (filename, contentType, length, open) =>
                {
                    var contentDisposition = new ContentDispositionHeaderValue(open ? "inline" : "attachment");
                    contentDisposition.SetHttpFileName(filename);

                    context.HttpContext.Response.ContentLength = length;
                    context.HttpContext.Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
                    context.HttpContext.Response.ContentType = contentType;
                });
            }
        }
    }
}