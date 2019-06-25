namespace Documents.Clients.Manager.Modules
{
    using BCrypt.Net;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Security;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.LEOUpload.Requests;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.ItemQueryHandlers;
    using Documents.Clients.Manager.Modules.ItemQueryHandlers.LEOUpload;
    using Documents.Clients.Manager.Modules.LEOUpload;
    using Documents.Clients.Manager.Services;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class LEOUploadModule : BaseModule, IModule
    {
        public override ModuleType ModuleType() => Modules.ModuleType.LEOUpload;

        protected readonly APIConnection connection;
        protected readonly APIConnection privilegedConnection;
        protected readonly ManagerConfiguration managerConfiguration;
        protected readonly IAuditLogStore auditLogStore;

        public LEOUploadModule(APIConnection connection, APIConnection priveligedConnection, ManagerConfiguration managerConfiguration, MetadataAuditLogStore auditLogStore)
        {
            this.connection = connection;
            this.managerConfiguration = managerConfiguration;
            // This privieged connection allows us to do things like add a user.
            this.privilegedConnection = priveligedConnection;
            this.auditLogStore = auditLogStore;
        }

        public override bool IsModuleActive(FolderModel folder)
        {
            //First we need to determine based on metadata whether leo upload is active for this folder.
            return folder.Read<bool>(MetadataKeyConstants.LEO_UPLOAD_ACTIVE_METAKEY);
        }

        public override List<PatternRegex> GetPathValidationPatterns()
        {
            return new List<PatternRegex> { new PatternRegex() { Pattern = "^\\b([lL][eE][oO] [uU]pload)\\b.*$", IsAllowed = false } };
        }

        public override void BuildDynamicFolders(PathIdentifier identifier, ItemQueryResponse page, FolderModel folder, bool isRestrictedView = false)
        {
            if (!isRestrictedView)
            {
                var leoUploadRootIdentifier = new PathIdentifier(identifier)
                {
                    PathKey = LEOUploadUtility.LEO_UPLOAD_PATH_KEY
                };


                // If it hasn't been added then it's going to be generated dynamically like this.
                if (!page?.PathTree?.Paths?.Any(p => p.Identifier.PathKey == LEOUploadUtility.LEO_UPLOAD_PATH_KEY) ?? false)
                {
                    if (page.PathTree.Paths == null)
                        page.PathTree.Paths = new List<ManagerPathModel>();

                    // Last the parent / root node of 'eDiscovery'
                    var leoUploadPath = new ManagerPathModel
                    {
                        Identifier = leoUploadRootIdentifier,
                        Icons = new string[] { LEOUploadUtility.LEO_UPLOAD_FOLDER_COLOR_STYLE },
                        Name = LEOUploadUtility.LEO_UPLOAD_PATH_NAME,
                        FullPath = LEOUploadUtility.LEO_UPLOAD_PATH_KEY,
                        AllowedOperations = null,
                        Paths = new List<ManagerPathModel>(),
                        IsExpanded = false // TODO: Put in the real logic here.
                    };

                    page.PathTree.Paths.Add(leoUploadPath);
                }
                else
                {
                    // In this case it already exists in the path tree, we're going to move it around, and alter some of it's properties.
                    // this also means that there's a child folder for an officer underneath the LEO Upload path.
                    // This is also where you might want to put some ordering in place. 
                    var leoUploadManagerPath = page.PathTree.Paths.Where(path => path.Identifier.PathKey == LEOUploadUtility.LEO_UPLOAD_PATH_KEY).FirstOrDefault();

                    // Now that we have the path, we're going to do things like change it's icon color.
                    if (leoUploadManagerPath != null)
                    {
                        leoUploadManagerPath.Icons = new string[] { LEOUploadUtility.LEO_UPLOAD_FOLDER_COLOR_STYLE };
                        leoUploadManagerPath.AllowedOperations = null;
                        leoUploadManagerPath.IsExpanded = true;
                    }

                    // We also want to color all the children blue as well.
                    foreach (var child in leoUploadManagerPath.Paths)
                    {
                        child.Icons = new string[] { LEOUploadUtility.LEO_UPLOAD_FOLDER_COLOR_STYLE };
                    }

                    // Now we need to move this order around.
                    page.PathTree.Paths.Remove(leoUploadManagerPath);

                    page.PathTree.Paths.Add(leoUploadManagerPath);

                }

                RecurseTree(page.PathTree, path =>
                {
                    if (path.Identifier.IsChildOf(leoUploadRootIdentifier) && (path.AllowedOperations?.Any() ?? false))
                        path.AllowedOperations = path.AllowedOperations.Where(a => !(a.BatchOperation is MoveIntoRequest));
                });

            }
        }

        public void RecurseTree(ManagerPathModel node, Action<ManagerPathModel> action)
        {
            if (node != null)
            {
                action(node);
                if (node.Paths?.Any() ?? false)
                    foreach (var child in node.Paths)
                        RecurseTree(child, action);
            }
        }

        public async Task<UserAuthenticatedResponse> AuthenticateUserAsync(AuthenticateUserRequest authenticateUserRequest, string linkEncryptionKey)
        {
            if (String.IsNullOrEmpty(authenticateUserRequest.Token))
            {
                return new UserAuthenticatedResponse() { IsAuthenticated = false };
            }

            // Get the magic link object back out from the token.
            var magicLink = ModuleUtility.DecryptMagicLink(authenticateUserRequest.Token, linkEncryptionKey);

            // Now we have a magic link object, we need to compare the email on the token, with the one that was passed in. 
            if (magicLink.RecipientEmail.ToLower() != authenticateUserRequest.Email.ToLower()
                || magicLink.ExipirationDate < DateTime.UtcNow // next let's check that this link hasn't expired.
               ) // Next we check to make sure the folder keys are the same.
            {
                return new UserAuthenticatedResponse() { IsAuthenticated = false };
            }

            // Before we can operate, and get any folder details, we're also going to need to authenticate the user in the backend. This will properly set things on the connection
            // such that it can make a backed call. 
            try
            {
                await connection.User.AuthenticateAsync(new TokenRequestModel
                {
                    Identifier = ModuleUtility.GetFolderScopedUserIdentifier(magicLink.FolderIdentifier, authenticateUserRequest.Email, "leo"),
                    Password = authenticateUserRequest.Password
                });

                connection.AddCookieTokenToResponse();

                // Now we can do the password check.  We're going to check that the password in our database matches on hash 
                // the password that came in on the request.
                var folder = await connection.Folder.GetAsync(magicLink.FolderIdentifier);

                // get the recipients, so we can find this particular recipient.
                var officers = folder.MetaLEOUploadOfficerListRead();
                var recipient = officers.Where(rec => rec.Email.ToLower() == magicLink.RecipientEmail.ToLower()).FirstOrDefault();

                // We check to make sure that there's a recipient on this folder that matches by email, and that the plain text
                // password that was passed in matches the password in our database. 
                if (recipient != null && BCrypt.Verify(authenticateUserRequest.Password, recipient.PasswordHash))
                {
                    await this.auditLogStore.AddEntry(
                            new AuditLogEntry()
                            {
                                EntryType = AuditLogEntryType.LEOUploadUserLogin,
                                Message = "An officer Has Logged in.",
                                ModuleType = Modules.ModuleType.LEOUpload
                            },
                            folder.Identifier,
                            connection
                            );

                    return new UserAuthenticatedResponse()
                    {
                        IsAuthenticated = true,
                        FolderIdentifier = folder.Identifier,
                        PathIdentifier = GetOfficerPath(folder.Identifier, recipient.FirstName, recipient.LastName)
                    };
                }
                else
                {
                    return new UserAuthenticatedResponse() { IsAuthenticated = false };
                }
            }
            catch (Exception)
            {
                return new UserAuthenticatedResponse() { IsAuthenticated = false };
            }
        }

        public async Task<PathIdentifier> GetOfficerPathAsync(FolderIdentifier folderIdentifier)
        {
            var user = await connection.User.GetAsync(connection.UserIdentifier);

            return GetOfficerPath(folderIdentifier, user.FirstName, user.LastName);
        }

        public PathIdentifier GetOfficerPath(FolderIdentifier folderIdentifier, string firstName, string lastName)
        {
            var leoUploadFolder = new PathIdentifier(folderIdentifier, LEOUploadUtility.LEO_UPLOAD_PATH_KEY);

            // Here we're adding a child folder to the leo upload whenever we add a new officer. 
            var childPath = leoUploadFolder.CreateChild($"{firstName} {lastName}");

            return childPath;
        }

        public async Task<RecipientResponse> AddRecipientAsync(AddOfficerRequest addOfficerRequest, string landingLocation, string passphrase)
        {
            var folder = await connection.Folder.GetAsync(addOfficerRequest.FolderIdentifier);

            DateTime? expirationDate = ModuleUtility.GetLinkExpirationDate(folder, MetadataKeyConstants.LEO_UPLOAD_EXPIRATION_LENGTH_SECONDS);

            var password = ModuleUtility.GeneratePassword(
                folder,
                MetadataKeyConstants.LEO_UPLOAD_RND_PASSWORD_LENGTH,
                LEOUploadUtility.LEO_UPLOAD_DEFAULT_PASSWORD_LENGTH,
                MetadataKeyConstants.LEO_UPLOAD_RND_PASSWORD_CHARS);

            // We're going to generate a user for eDicsovery.  This user will have restricted priveleges. 
            var user = await GenerateUser(addOfficerRequest, password.Plain);

            string completeUrl = ModuleUtility.CreateMagicLink(addOfficerRequest, landingLocation, passphrase, folder.Identifier, expirationDate, user.Identifier);

            await connection.ConcurrencyRetryBlock(async () =>
            {
                folder = await connection.Folder.GetAsync(addOfficerRequest.FolderIdentifier);

                folder.MetaLEOUploadOfficerListUpsert(new ExternalUser()
                {
                    Email = addOfficerRequest.RecipientEmail,
                    FirstName = addOfficerRequest.FirstName,
                    LastName = addOfficerRequest.LastName,
                    PasswordHash = password.Hashed,
                    MagicLink = completeUrl,
                    ExpirationDate = expirationDate.GetValueOrDefault()
                });

                var childPath = GetOfficerPath(addOfficerRequest.FolderIdentifier, addOfficerRequest.FirstName, addOfficerRequest.LastName);

                var allPaths = folder.Read("_paths", defaultValue: new List<string>());

                allPaths.Add(childPath.FullName);

                folder.Write("_paths", allPaths);

                await connection.Folder.PutAsync(folder);
            });

            await this.auditLogStore.AddEntry(
                new AuditLogEntry()
                {
                    EntryType = AuditLogEntryType.LEOUploadOfficerAdded,
                    Message = $"A LEO officer has been added. {addOfficerRequest.RecipientEmail}",
                    ModuleType = Modules.ModuleType.LEOUpload
                },
                folder.Identifier
            );

            // build up the response
            return new RecipientResponse()
            {
                Email = addOfficerRequest.RecipientEmail,
                ExpirationDate = expirationDate.GetValueOrDefault(),
                MagicLink = completeUrl,
                Password = password.Plain,
                FirstName = addOfficerRequest.FirstName,
                LastName = addOfficerRequest.LastName,
            };
        }

        private async Task<UserModel> GenerateUser(RecipientRequestBase addRecipientRequest, string password)
        {
            await InitializePrivilegedConnectionAsync();

            var folderIdentifier = addRecipientRequest.FolderIdentifier;
            var userModel = new UserModel
            {
                Identifier = ModuleUtility.GetFolderScopedUserIdentifier(folderIdentifier, addRecipientRequest.RecipientEmail, "leo"),
                EmailAddress = addRecipientRequest.RecipientEmail,
                FirstName = addRecipientRequest.FirstName,
                LastName = addRecipientRequest.LastName,
            };

            userModel = await privilegedConnection.User.PostAsync(userModel);
            await privilegedConnection.User.PasswordPutAsync(userModel.Identifier, password);

            await privilegedConnection.User.AccessIdentifiersPutAsync(userModel.Identifier, new[] {
                "r:leoUpload",
                $"o:{folderIdentifier.OrganizationKey}",
                "x:pcms",
                "x:eDiscovery",
                $"u:{userModel.Identifier.UserKey.Replace(" ", "_")}"
            });

            return userModel;
        }

        private async Task InitializePrivilegedConnectionAsync()
        {
            // Now we use the special priveliged connection to update users/recipients.
            await privilegedConnection.User.AuthenticateAsync(new TokenRequestModel
            {
                Identifier = new UserIdentifier
                {
                    OrganizationKey = managerConfiguration.LEOUploadImpersonationOrganization,
                    UserKey = managerConfiguration.LEOUploadImpersonationUser
                },
                Password = managerConfiguration.LEOUploadImpersonationPasssword
            });
        }

        public override async Task<ModelBase> HandleBatchOperation(ModelBase operation)
        {
            if (operation is RemoveOfficerRequest)
            {
                var remove = operation as RemoveOfficerRequest;
                return await RemoveOfficerAsync(remove);
            }

            if (operation is RegenerateOfficerRequest)
            {
                var regen = operation as RegenerateOfficerRequest;
                return await RegenerateOfficerAsync(regen);
            }

            return null;
        }

        private async Task<ModelBase> RegenerateOfficerAsync(RegenerateOfficerRequest regenRequest)
        {
            var folder = await connection.Folder.GetAsync(regenRequest.FolderIdentifier);
            var recipients = folder.MetaLEOUploadOfficerListRead();

            var password = ModuleUtility.GeneratePassword(folder,
                    MetadataKeyConstants.LEO_UPLOAD_RND_PASSWORD_LENGTH,
                    LEOUploadUtility.LEO_UPLOAD_DEFAULT_PASSWORD_LENGTH,
                    MetadataKeyConstants.LEO_UPLOAD_RND_PASSWORD_CHARS);

            var recipient = recipients.Where(rec => rec.Email.ToLower() == regenRequest.RecipientEmail.ToLower()).FirstOrDefault();
            if (recipient != null)
            {
                await InitializePrivilegedConnectionAsync();

                var userIdentifier = ModuleUtility.GetFolderScopedUserIdentifier(folder.Identifier, regenRequest.RecipientEmail, "leo");

                // Using the special connection here to update their password. 
                await privilegedConnection.User.PasswordPutAsync(userIdentifier, password.Plain);

                await this.auditLogStore.AddEntry(
                    new AuditLogEntry()
                    {
                        EntryType = AuditLogEntryType.LEOUploadOfficerRegenerated,
                        Message = $"An leo officer has had their password regenerated {recipient.Email}",
                        ModuleType = Modules.ModuleType.LEOUpload
                    },
                    folder.Identifier
                );

                recipient.PasswordHash = password.Hashed;
                recipient.ExpirationDate = ModuleUtility.GetLinkExpirationDate(folder, MetadataKeyConstants.LEO_UPLOAD_EXPIRATION_LENGTH_SECONDS).Value;
                recipient.MagicLink = ModuleUtility.CreateMagicLink(
                    regenRequest, 
                    managerConfiguration.LEOUploadLandingLocation,
                    managerConfiguration.LEOUploadLinkEncryptionKey, 
                    folder.Identifier, 
                    recipient.ExpirationDate,
                    userIdentifier
                );

                await connection.ConcurrencyRetryBlock(async () =>
                {
                    folder = await connection.Folder.GetAsync(regenRequest.FolderIdentifier);

                    folder.MetaLEOUploadOfficerListUpsert(recipient);

                    await connection.Folder.PutAsync(folder);
                });

                // now we also want to send back the magic link that we generated before.
                return new RecipientResponse()
                {
                    Email = regenRequest.RecipientEmail,
                    FirstName = recipient.FirstName,
                    LastName = recipient.LastName,
                    Password = password.Plain,
                    MagicLink = recipient.MagicLink,
                    ExpirationDate = recipient.ExpirationDate,
                };
            }

            return new RecipientResponse();
        }

        private async Task<ModelBase> RemoveOfficerAsync(RemoveOfficerRequest removeRequest)
        {
            FolderModel folder = null;

            await connection.ConcurrencyRetryBlock(async () =>
            {
                folder = await connection.Folder.GetAsync(removeRequest.FolderIdentifier);
                var recipients = folder.MetaLEOUploadOfficerListRead();

                var recipient = recipients.FirstOrDefault(f => f.Email == removeRequest.RecipientEmail);

                folder.MetaLEOUploadOfficerListRemove(removeRequest.RecipientEmail);

                var paths = folder.Read<List<string>>("_paths");
                if (paths != null && recipient != null)
                {
                    var officerPath = GetOfficerPath(folder.Identifier, recipient.FirstName, recipient.LastName);
                    if (paths.Contains(officerPath.PathKey))
                        paths = paths.Where(p => !p.StartsWith(officerPath.PathKey)).ToList();

                    folder.Write("_paths", paths);
                }

                await connection.Folder.PutAsync(folder);
            });

            await InitializePrivilegedConnectionAsync();

            // Using the special connection here to delete the user. 
            await privilegedConnection.User.DeleteAsync(ModuleUtility.GetFolderScopedUserIdentifier(folder.Identifier, removeRequest.RecipientEmail, "leo"));

            await this.auditLogStore.AddEntry(
                new AuditLogEntry()
                {
                    EntryType = AuditLogEntryType.LEOUploadOfficerDeleted,
                    Message = $"An officer has been removed {removeRequest.RecipientEmail}",
                    ModuleType = Modules.ModuleType.LEOUpload
                },
                folder.Identifier
            );

            return new RecipientResponse()
            {
                Email = removeRequest.RecipientEmail,
            };
        }

        public override void OverrideAllowedOperations(FileModel fileModel, List<AllowedOperation> allowed, PathIdentifier virtualPathIdentifier)
        {
            var isUserLeo = LEOUploadUtility.IsUserLeo(connection.UserAccessIdentifiers);

            var leoUploadFolder = new PathIdentifier(fileModel.Identifier as FolderIdentifier, LEOUploadUtility.LEO_UPLOAD_PATH_KEY);
            if (virtualPathIdentifier.Equals(leoUploadFolder) || virtualPathIdentifier.IsChildOf(leoUploadFolder))
            {
                allowed.Clear();

                allowed.Add(AllowedOperation.GetAllowedOperationDownload(fileModel.Identifier, false));
                allowed.Add(AllowedOperation.GetAllowedOperationMove(fileModel.Identifier));

                if (isUserLeo)
                {
                    allowed.Add(AllowedOperation.GetAllowedOperationRename(fileModel.Identifier));
                }

            }
        }

        public override bool HasHandlerForBatchOperation(ModelBase operation)
        {
            if (operation is RemoveOfficerRequest
                || operation is RegenerateOfficerRequest
                )
            {
                return true;
            }
            return false;
        }

        public override BaseItemQueryHandler GetInitialQueryHandler(PathIdentifier identifier, PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService)
        {
            // If the user is leo user, we're going to use a special query handler that 
            // will return only the files the user is allowed to see.
            if (LEOUploadUtility.IsUserLeo(connection.UserAccessIdentifiers))
            {
                var userKey = connection.UserIdentifier.UserKey;

                // here we're going to do some basic security check.  The userKey has a folder key on it.  We need to make sure that matches.
                var userKeyTokens = userKey.Split(':').ToList();
                // now this will be an array, of tokens, keep in mind userkeys can be something like Defendant:18337980:support1@nypti.org, so we want everything before the last ':'
                // so we can remove the last token, and then we'll be able to just join back everything else.
                userKeyTokens.RemoveAt(userKeyTokens.Count - 1);
                // also get rid of the leo: prefix
                userKeyTokens.RemoveAt(0);
                var folderKeyFromUser = String.Join(':', userKeyTokens);
                if (folderKeyFromUser != identifier.FolderKey)
                {
                    throw (new UnauthorizedAccessException($"You're not authorized for this Folder userKey:{userKey} tried to access: {identifier.FolderKey}"));
                }

                return new LEOUserItemQueryHandler(pathService, connection, auditLogStore, this, managerConfiguration, fileService);
            }

            if (identifier.PathKey == LEOUploadUtility.LEO_UPLOAD_PATH_KEY)
            {
                return new LEOUploadRootQueryHandler(pathService, connection, auditLogStore, this, managerConfiguration, fileService);
            }

            return null;
        }

        

        public async override Task PreUploadAsync(FolderModel folderModel, FileModel fileModel)
        {
            if (LEOUploadUtility.IsUserLeo(connection.UserAccessIdentifiers))
            {
                var childPath = await GetOfficerPathAsync(folderModel.Identifier);

                var attemptedPath = fileModel.MetaPathIdentifierRead();

                bool pathAllowed = (attemptedPath.IsChildOf(childPath) || attemptedPath.Equals(childPath));

                if (!pathAllowed)
                    fileModel.MetaPathIdentifierWrite(childPath);

            }
        }

        public async override Task PostUploadAsync(FolderModel folderModel, FileModel fileModel)
        {
            if (LEOUploadUtility.IsUserLeo(connection.UserAccessIdentifiers))
            {
                await connection.ConcurrencyRetryBlock(async () =>
                {
                    fileModel.WriteACLs("read", new[] {
                        new ACLModel
                        {
                            OverrideKey = "leo",
                            RequiredIdentifiers = new List<string>
                            {
                                "u:system",
                                "x:leo",
                                $"u:{connection.UserIdentifier.UserKey.Replace(" ", "_")}"
                            }
                        }
                    });

                    await connection.File.PutAsync(fileModel);
                });

                var message = "Officer uploaded file: " + fileModel.Name;

                await this.auditLogStore.AddEntry(
                    new AuditLogEntry()
                    {
                        EntryType = AuditLogEntryType.LEOUploadOfficerUpload,
                        Message = message,
                        ModuleType = Modules.ModuleType.LEOUpload
                    },
                    fileModel.Identifier,
                    connection
                );

                await connection.Log.PostAsync(new AuditLogEntryModel
                {
                    Identifier = new AuditLogEntryIdentifier(fileModel.Identifier),
                    FileIdentifier = fileModel.Identifier,
                    ActionType = "LEOUpload",
                    Description = message,
                    Details = JsonConvert.SerializeObject(fileModel.Identifier),
                    InitiatorUserIdentifier = connection.UserIdentifier,
                    Generated = DateTime.UtcNow,
                    UserAgent = connection.UserAgent
                });
            }
        }

        public override void FinalFilter(List<IItemQueryResponse> allRows, PathIdentifier identifier)
        {
            var leoUploadPath = new PathIdentifier(identifier as FolderIdentifier, LEOUploadUtility.LEO_UPLOAD_PATH_KEY);

            if (identifier.IsRoot)
            {
                var subPath = allRows.OfType<ManagerPathModel>().FirstOrDefault(r => leoUploadPath.Equals(r.Identifier));
                if (subPath != null)
                    allRows.Remove(subPath);
            }
        }

        public override void OnResponse(ItemQueryResponse response, PathIdentifier pathIdentifier)
        {
            var leoUploadPath = new PathIdentifier(pathIdentifier as FolderIdentifier, LEOUploadUtility.LEO_UPLOAD_PATH_KEY);
            if (pathIdentifier.IsChildOf(leoUploadPath) && response.AllowedOperations != null)
                if (!LEOUploadUtility.IsUserLeo(connection.UserAccessIdentifiers))
                    response.AllowedOperations = response.AllowedOperations.Where(a => !(a.BatchOperation is UploadRequest));
        }
    }
}
