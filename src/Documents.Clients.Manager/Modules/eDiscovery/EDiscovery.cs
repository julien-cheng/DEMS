namespace Documents.Clients.Manager.Modules
{
    using BCrypt.Net;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Security;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Common.PathStructure;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.eDiscovery.Responses;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.Requests.eDiscovery;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Modules.ItemQueryHandlers;
    using Documents.Clients.Manager.Services;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class EDiscovery : BaseModule, IModule
    {
        protected readonly APIConnection connection;
        protected readonly APIConnection privilegedConnection;
        protected readonly ManagerConfiguration managerConfiguration;
        protected readonly IAuditLogStore auditLogStore;

        public override ModuleType ModuleType() => Modules.ModuleType.eDiscovery;

        public EDiscovery(APIConnection connection, APIConnection priveligedConnection, ManagerConfiguration managerConfiguration, MetadataAuditLogStore auditLogStore)
        {
            this.connection = connection;
            this.managerConfiguration = managerConfiguration;
            // This priveleged connection is used specifically when we're upgrading to the edisc credentials.  We really don't want anything else 
            // in the pipeline to be able to use this.  So to be safe we're creating a seperate connection in this case. 
            this.privilegedConnection = priveligedConnection;

            this.auditLogStore = auditLogStore;
        }

        #region IModule Overrides 
        public override bool IsModuleActive(FolderModel folder)
        {
            //First we need to determine based on metadata whether E-Discovery is active for this folder.
            return folder.Read<bool>(MetadataKeyConstants.E_DISCOVERY_ACTIVE_METAKEY);
        }

        public override void OverrideAllowedOperations(FileModel fileModel, List<AllowedOperation> allowed, PathIdentifier virtualPathIdentifier)
        {
            if (EDiscoveryUtility.IsEDiscoveryPath(virtualPathIdentifier))
            {
                allowed.Clear();
                allowed.Add(
                    AllowedOperation.GetAllowedOperationDownload(fileModel.Identifier, false)
                );
                allowed.Add(
                    AllowedOperation.GetAllowedOperationDownloadZip(fileModel.Identifier)
                );
            }

            // The allowed operations depend on what state the file is in. 
            switch (EDiscoveryUtility.GetCurrentShareState(fileModel))
            {
                case EDiscoveryShareState.NotShared:
                    // If the file hasn't been shared yet, we can "Share" it.
                    allowed.Add(AllowedOperation.GetAllowedOperationShare(fileModel.Identifier, true));
                    break;
                case EDiscoveryShareState.Staged:
                    // If the file has only been staged, the we can "unshare" it.  
                    allowed.Add(AllowedOperation.GetAllowedOperationShare(fileModel.Identifier, false));
                    break;
                case EDiscoveryShareState.Published:
                    // If it's been published there isn't anything related to sharing that you can do on the file. 
                    // However if it's published we're not going to allow you to delete the file.
                    allowed.RemoveAll(action => action.BatchOperation is DeleteRequest);
                    allowed.RemoveAll(action => action.BatchOperation is RenameRequest);
                    break;
                default:
                    break;
            }
        }

        public override void BuildDynamicFolders(PathIdentifier identifier, ItemQueryResponse page, FolderModel folder, bool isRestrictedView)
        {
            if (!page?.PathTree?.Paths?.Any(p => p.Identifier.PathKey == EDiscoveryUtility.E_DISCOVERY_PATH_KEY) ?? false)
            {
                if (page.PathTree.Paths == null)
                    page.PathTree.Paths = new List<ManagerPathModel>();

                var managerPathModels = new List<ManagerPathModel>();

                // we have to build up the children first.
                // First we get all the dated package dynamic paths.
                var datedPackagePaths = BuildDatedPackagesDynamicFolder(folder);

                var eDiscoveryIdentifier = new PathIdentifier(identifier)
                {
                    PathKey = EDiscoveryUtility.E_DISCOVERY_PATH_KEY
                };

                // Last the parent / root node of 'eDiscovery'
                var eDiscoveryPath = new ManagerPathModel
                {
                    Identifier = eDiscoveryIdentifier,
                    Icons = new string[] { EDiscoveryUtility.EDISCOVERY_FOLDER_COLOR_STYLE },
                    Name = EDiscoveryUtility.E_DISCOVERY_PATH_NAME,
                    FullPath = EDiscoveryUtility.E_DISCOVERY_PATH_KEY,
                    AllowedOperations = null,
                    Paths = new List<ManagerPathModel>(),
                    IsExpanded = datedPackagePaths.Count > 0 || EDiscoveryUtility.GetStagedCount(folder) > 0,
                };

                if (datedPackagePaths.Any())
                {
                    var allPackagesIdentifier = new PathIdentifier(identifier as FolderIdentifier, EDiscoveryUtility.E_DISCOVERY_ALL_PACKAGE_PATH_KEY);

                    var packageName = "All";

                    var processor = new PathProcessor(folder.Identifier);
                    processor.Read(folder, skipFolderPaths: true, pathReader: f => {
                        var sharePath = f.MetaEDiscoveryPathIdentifierRead();

                        if (sharePath != null)
                            return allPackagesIdentifier.CreateChild(sharePath.PathKey);
                        else
                            return sharePath;
                    });


                    datedPackagePaths.Insert(0, new EDiscoveryManagerPathModel()
                    {
                        Identifier = allPackagesIdentifier,
                        Icons = new string[] { EDiscoveryUtility.EDISCOVERY_FOLDER_COLOR_STYLE },
                        Name = packageName,
                        FullPath = allPackagesIdentifier.FullName,
                        Paths = processor[allPackagesIdentifier]?.Paths
                    });
                }

                eDiscoveryPath.Paths = eDiscoveryPath.Paths.Concat(datedPackagePaths).ToList();

                if (!isRestrictedView)
                {
                    eDiscoveryPath.Paths.Add(GetNotSharedYetPath(folder));
                }

                page.PathTree.Paths.Add(eDiscoveryPath);
            }
        }

        public override List<PatternRegex> GetPathValidationPatterns()
        {
            return new List<PatternRegex> { new PatternRegex() { Pattern = "^\\b([eE][dD]iscovery|[nN]ot [sS]hared [yY]et|[dD]iscovery [pP]ackage)\\b.*$", IsAllowed = false } };
        }

        public override BaseItemQueryHandler GetInitialQueryHandler(PathIdentifier identifier, PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService)
        {

            // If the user is an eDiscovery user, we're going to use a special query handler that will return only the files the user is allowed to see.
            if (EDiscoveryUtility.IsUserEDiscovery(connection.UserAccessIdentifiers))
            {
                var userKey = connection.UserIdentifier.UserKey;

                // here we're going to do some basic security check.  The userKey has a folder key on it.  We need to make sure that matches.
                var userKeyTokens = userKey.Split(':').ToList();
                // now this will be an array, of tokens, keep in mind userkeys can be something like Defendant:18337980:support1@nypti.org, so we want everything before the last ':'
                // so we can remove the last token, and then we'll be able to just join back everything else.
                userKeyTokens.RemoveAt(userKeyTokens.Count - 1);
                var folderKeyFromUser = String.Join(':', userKeyTokens);
                if (folderKeyFromUser != identifier.FolderKey)
                    throw (new UnauthorizedAccessException($"You're not authorized for this eDiscovery Folder userKey:{userKey} tried to access: {identifier.FolderKey}"));

                return new EDiscoveryUserItemQueryHandler(pathService, connection, auditLogStore, this, managerConfiguration, fileService);
            }

            if (identifier.PathKey == EDiscoveryUtility.E_DISCOVERY_PATH_KEY)
            {
                return new EDiscoveryRootQueryHandler(pathService, connection, auditLogStore, this, managerConfiguration, fileService);
            }

            if (identifier.PathKey != null &&
                identifier.FullName.StartsWith(EDiscoveryUtility.E_DISCOVERY_NOT_SHARED_PATH_KEY))
            {
                return new EDiscoveryStagedItemQueryHandler(pathService, connection, managerConfiguration, fileService);
            }

            if (identifier.PathKey != null && 
                (identifier.FullName.StartsWith(EDiscoveryUtility.E_DISCOVERY_DATED_PACKAGE_PATH_KEY)
                 || identifier.FullName.StartsWith(EDiscoveryUtility.E_DISCOVERY_ALL_PACKAGE_PATH_KEY)))
                    return new EDiscoveryDatedPackageItemQueryHandler(pathService, connection, managerConfiguration, fileService);

            return null;
        }

        public override void AlterGridTitle(string gridTitle, PathIdentifier identifier)
        {
            if (EDiscoveryUtility.IsEDiscoveryPath(identifier))
            {
                gridTitle = identifier.FullName;
            }
        }

        public override void AlterSubPathOperations(PathIdentifier pathIdentifier, List<AllowedOperation> subPathDefaultOperations)
        {
            if (EDiscoveryUtility.IsEDiscoveryPath(pathIdentifier))
            {
                // If it's an eDiscovery path, we're going to clear out the allowed operations.
                subPathDefaultOperations.Clear();
            }
            else
            {
                subPathDefaultOperations.Add(AllowedOperation.GetAllowedOperationShare(pathIdentifier, true));
                subPathDefaultOperations.Add(AllowedOperation.GetAllowedOperationShare(pathIdentifier, false));
            }
        }

        public override async Task<ModelBase> HandleBatchOperation(ModelBase operation)
        {
            if (operation is EditPackageNameRequest)
            {
                var editPackageName = operation as EditPackageNameRequest;
                await EditPackageNameRequest(editPackageName);
            }

            if (operation is RemoveRecipientRequest)
            {
                var remove = operation as RemoveRecipientRequest;
                return await RemoveRecipientAsync(remove);
            }

            if (operation is RegenerateRecipientPasswordRequest)
            {
                var regen = operation as RegenerateRecipientPasswordRequest;
                return await RegenerateRecipientPasswordAsync(regen);
            }

            if (operation is ShareRequest share)
            {
                if (share.FileIdentifier != null)
                    await this.ShareFile(share.FileIdentifier, true);
                else if (share.PathIdentifier != null)
                    await this.SharePath(share.PathIdentifier, true);
            }
            if (operation is UnshareRequest unshare)
            {
                if (unshare.FileIdentifier != null)
                    await this.ShareFile(unshare.FileIdentifier, false);
                else if (unshare.PathIdentifier != null)
                    await this.SharePath(unshare.PathIdentifier, false);
            }

            if (operation is PublishRequest)
            {
                var publish = operation as PublishRequest;
                await PublishFolder(publish.FolderIdentifier, publish.CustomName);
            }

            return null;
        }

        public override bool HasHandlerForBatchOperation(ModelBase operation)
        {
            if (operation is EditPackageNameRequest
                || operation is RemoveRecipientRequest
                || operation is RegenerateRecipientPasswordRequest
                || operation is ShareRequest
                || operation is UnshareRequest
                || operation is PublishRequest
                )
            {
                return true;
            }
            return false;
        }
        #endregion

        public async Task ShareFile(FileIdentifier fileIdentifier, bool shared)
        {
            if (shared)
                await ShareFileForEDiscoveryAsync(fileIdentifier);
            else
                await UnShareFileForEDiscoveryAsync(fileIdentifier);
        }

        public async Task SharePath(PathIdentifier pathIdentifier, bool shared)
        {
            if (shared)
                await SharePathForEDiscoveryAsync(pathIdentifier);
            else
                await UnSharePathForEDiscoveryAsync(pathIdentifier);
        }


        public async Task GenerateManifest(List<FileModel> filesToPublish, PathIdentifier manifestDestination, string packageName, string packageDate)
        {

            var manifestName = $"Compliance Report-{packageName.Replace("/", "-").Replace(":", "")}.pdf";

            using (var ms = new MemoryStream())
            {
                var manifestEntries = new List<ManifestEntry>();
                foreach(var file in filesToPublish)
                {
                    manifestEntries.Add(new ManifestEntry() {
                        Name = file.Name,
                        Size = file.LengthForHumans,
                        Path = file.MetaEDiscoveryPathIdentifierRead()?.PathKey,
                    });
                }
                await ManifestGenerator.Generate(ms, packageName, manifestEntries);

                // Move the stream back to zero.
                ms.Seek(0, SeekOrigin.Begin);

                // Now we send our manifest up to the api.
                var newFile = new FileModel
                {
                    Identifier = new FileIdentifier(manifestDestination as FolderIdentifier, null),
                    Name = manifestName,
                    Length = ms.Length,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                    MimeType = "application/pdf"
                };
                newFile.InitializeEmptyMetadata();

                newFile.MetaPathIdentifierWrite(manifestDestination);

                UpdateShareState(newFile, EDiscoveryShareState.Published);
                TagSharePackage(newFile, packageName);

                newFile = await this.connection.File.PostAsync(newFile);

                newFile = await this.connection.File.UploadAsync(newFile, ms);
            }
        }

        /// <summary>
        /// This will move all the files from the not shared yet folder, and change their state to published. Adding comment
        /// </summary>
        public async Task PublishFolder(FolderIdentifier folderIdentifier, string customName)
        {
            // Setup our state so we have everything we need. 
            var folder = await connection.Folder.GetAsync(folderIdentifier, new List<PopulationDirective>
            {
                new PopulationDirective
                {
                    Name = nameof(FolderModel.Files)
                }
            });

            var packageDate = DateTime.UtcNow.ConvertToLocal(connection.UserTimeZone).ToString("MM/dd/yyyy hh:mm");

            // we need to create a dated package into which will move these files.  
            var packageName = "Discovery Package: " + packageDate;

            // we need to find all the files that are currently in the "Not Yet Shared" Folder.
            var allFiles = folder.Files.Rows.ToList(); //A collection of files that we will use to build up our response.

            // We need a list of files that we published, so we can generate our manifest
            var publishedFiles = new List<FileModel>();
            foreach (var file in allFiles)
            {
                // Get the current share state of the file
                var shareState = file.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY);

                //TODO Check to make sure this .toString() on the enum will filter correctly.
                if (shareState != null && shareState == EDiscoveryShareState.Staged.ToString())
                {
                    // commenting this out for now.  I really want to work on how I should create these dated packages.
                    UpdateShareState(file, EDiscoveryShareState.Published);
                    TagSharePackage(file, packageName);

                    await connection.File.PutAsync(file);
                    publishedFiles.Add(file);
                }
            }

            await UpsertPackageMap(folder, packageName, customName);

            await this.GenerateManifest(
                publishedFiles,
                new PathIdentifier(folderIdentifier, "Compliance Reports"),
                packageName,
                packageDate
            );

            await this.auditLogStore.AddEntry(
                new AuditLogEntry()
                {
                    EntryType = AuditLogEntryType.eDiscoveryPackageCreated,
                    Message = "A Discovery Package was created.",
                    ModuleType = Modules.ModuleType.eDiscovery
                },
                folderIdentifier
            );
        }

        private async Task UpsertPackageMap(FolderModel folder, string packageName, string customName)
        {
            // Here we're going to create a package map if one doesn't exist.
            var packageMap = folder.Read<EDiscoveryPackageMap>(MetadataKeyConstants.E_DISCOVERY_PACKAGE_MAP_METAKEY);
            // If this is the first package, we're going to create a package map.
            if (packageMap == null)
            {
                packageMap = new EDiscoveryPackageMap
                {
                    Map = new Dictionary<string, PackageAttributes>()
                };
            }
            packageMap.Map.Add(packageName, new PackageAttributes() { CustomName = customName});

            folder.Write<EDiscoveryPackageMap>(MetadataKeyConstants.E_DISCOVERY_PACKAGE_MAP_METAKEY, packageMap);
            await connection.Folder.PutAsync(folder);
        }

        public async Task EditPackageNameRequest(EditPackageNameRequest editPackageNameRequest)
        {
            // Setup our state so we have everything we need. 
            var folder = await connection.Folder.GetAsync(editPackageNameRequest.FolderIdentifier);

            // Here we're going to create a package map if one doesn't exist.
            var packageMap = folder.Read<EDiscoveryPackageMap>(MetadataKeyConstants.E_DISCOVERY_PACKAGE_MAP_METAKEY);
            // If this is the first package, we're going to create a package map.
            if (packageMap == null)
            {
                packageMap = new EDiscoveryPackageMap
                {
                    Map = new Dictionary<string, PackageAttributes>()
                };
            }

            // If this is the first package, we're going to create a package map.
            packageMap.Map[editPackageNameRequest.PackageName] = new PackageAttributes() { CustomName = editPackageNameRequest.CustomName };

            folder.Write<EDiscoveryPackageMap>(MetadataKeyConstants.E_DISCOVERY_PACKAGE_MAP_METAKEY, packageMap);
            await connection.Folder.PutAsync(folder);
        }

        public EDiscoveryManagerPathModel GetNotSharedYetPath(FolderModel folder)
        {
            // We need to get the staged count, as it will allow us to A. determine if we're going to show the not shared yet folder,
            // and B. a cound on the not shared yet folder.
            var stagedCount = EDiscoveryUtility.GetStagedCount(folder);
            var notSharedIdentifier = new PathIdentifier(folder.Identifier, EDiscoveryUtility.E_DISCOVERY_NOT_SHARED_PATH_KEY);

            var processor = new PathProcessor(folder.Identifier);
            processor.Read(folder, skipFolderPaths: true, pathReader: f => {

                if (f.ShareState() != EDiscoveryShareState.Staged)
                    return null;

                var sharePath = f.MetaEDiscoveryPathIdentifierRead();

                if (sharePath != null)
                    return notSharedIdentifier.CreateChild(sharePath.PathKey);

                return null;
            });

            return new EDiscoveryManagerPathModel()
            {
                Identifier = notSharedIdentifier,
                Icons = new string[] { EDiscoveryUtility.EDISCOVERY_FOLDER_COLOR_STYLE },
                Name = $"Not Shared Yet ({stagedCount.ToString()})",
                FullPath = "eDiscovery/Not Shared Yet",
                AllowedOperations = null,
                Paths = processor[notSharedIdentifier]?.Paths
            };
        }

        public List<EDiscoveryManagerPathModel> BuildDatedPackagesDynamicFolder(FolderModel folder)
        {
            var managerPaths = new List<EDiscoveryManagerPathModel>();

            var packageMap = folder.Read<EDiscoveryPackageMap>(MetadataKeyConstants.E_DISCOVERY_PACKAGE_MAP_METAKEY);

            if (this.IsModuleActive(folder))
            {
                // go through all the folders, and find all the 
                foreach (var file in folder.Files.Rows
                    .Where(f => f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE) != null)
                )
                {
                    var sharePackageName = file.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE);
                    var sharePackageIdentifier = new PathIdentifier(file.Identifier as FolderIdentifier, "eDiscovery/" + sharePackageName);

                    // Here we're checking to make sure we haven't already added this 'dated share folder'
                    if (!managerPaths.Any(mp => mp.Identifier.Equals(sharePackageIdentifier)))
                    {
                        var processor = new PathProcessor(folder.Identifier);
                        processor.Read(folder, skipFolderPaths: true, pathReader: f => {
                            var sharePath = f.MetaEDiscoveryPathIdentifierRead();
                            if (sharePackageName != null
                                && f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE) != sharePackageName)
                                return null;

                            if (sharePath != null)
                                return sharePackageIdentifier.CreateChild(sharePath.PathKey);
                            else
                                return sharePath;
                        });

                        var packagePath = new EDiscoveryManagerPathModel()
                        {
                            Identifier = sharePackageIdentifier,
                            Icons = new string[] { EDiscoveryUtility.EDISCOVERY_FOLDER_COLOR_STYLE },
                            Name = sharePackageName,
                            FullPath = sharePackageIdentifier.FullName,
                            Paths = processor[sharePackageIdentifier]?.Paths,
                            CustomName = EDiscoveryUtility.GetCustomName(packageMap, sharePackageName),
                            AllowedOperations = EDiscoveryUtility.IsUserEDiscovery(connection.UserAccessIdentifiers) ? new AllowedOperation[] { } : new AllowedOperation[] { AllowedOperation.GetAllowedOperationEditPackageName(folder.Identifier, sharePackageName) },
                        };
                        managerPaths.Add(packagePath);

                    }
                }
            }

            return managerPaths;
        }

        public async Task UnSharePathForEDiscoveryAsync(PathIdentifier pathIdentifier)
        {

            var staged = await this.FilesOfStateInPath(EDiscoveryShareState.Staged, pathIdentifier);
            foreach (var file in staged)
            {
                var filePath = file.MetaPathIdentifierRead();
                var relativePath = filePath.RelativeTo(pathIdentifier.ParentPathIdentifier);

                file.MetaEDiscoveryPathIdentifierWrite(null);

                UpdateShareState(file, EDiscoveryShareState.NotShared);
                await connection.File.PutAsync(file);
            }
        }

        private async Task<IEnumerable<FileModel>> FilesOfStateInPath(EDiscoveryShareState state, PathIdentifier pathIdentifier)
        {
            var folder = await connection.Folder.GetAsync(pathIdentifier, new List<PopulationDirective>
            {
                new PopulationDirective
                {
                    Name = nameof(FolderModel.Files)
                }
            });

            return folder.Files.Rows
                .Where(f => f.MetaPathIdentifierRead().IsChildOf(pathIdentifier))
                .Where(f => f.ShareState() == state);
        }

        public async Task SharePathForEDiscoveryAsync(PathIdentifier pathIdentifier)
        {
            var notShared = await this.FilesOfStateInPath(EDiscoveryShareState.NotShared, pathIdentifier);
            foreach (var file in notShared)
            {
                var filePath = file.MetaPathIdentifierRead();
                var relativePath = filePath.RelativeTo(pathIdentifier.ParentPathIdentifier);

                file.MetaEDiscoveryPathIdentifierWrite(relativePath);

                UpdateShareState(file, EDiscoveryShareState.Staged);
                await connection.File.PutAsync(file);
            }
        }

        public async Task ShareFileForEDiscoveryAsync(FileIdentifier fileIdentifier)
        {
            // We need to grab the details of the file first, so we can see what it's current state is.
            var file = await connection.File.GetAsync(fileIdentifier);

            var shareState = file.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY);

            // If there isn't a share state metadata on this file, then we're going to move it to staged.
            if (shareState == null)
            {
                UpdateShareState(file, EDiscoveryShareState.Staged);
            }
            else
            {
                switch ((EDiscoveryShareState)Enum.Parse(typeof(EDiscoveryShareState), shareState))
                {
                    case EDiscoveryShareState.NotShared:
                        UpdateShareState(file, EDiscoveryShareState.Staged);
                        break;
                    case EDiscoveryShareState.Staged:
                        UpdateShareState(file, EDiscoveryShareState.Published);
                        break;
                    case EDiscoveryShareState.Published:
                        // This is a noOp.  It shouldn't even be possible to call "share" on a file that has already been sent, but
                        // I want to call this out specifically as a noOp.
                        break;
                }
            }

            await connection.File.PutAsync(file);
        }

        public async Task UnShareFileForEDiscoveryAsync(FileIdentifier fileIdentifier)
        {
            // We need to grab the details of the file first, so we can see what it's current state is.
            var file = await connection.File.GetAsync(fileIdentifier);

            var shareState = EDiscoveryUtility.GetCurrentShareState(file);

            // The only case where you can pull a file back from shared, is if it's been staged.  In which case you can move it back to not shared yet.
            // If the file hasn't been shared, or if it's moved fully to shared, then we're not going to do anything. 
            if (shareState == EDiscoveryShareState.Staged)
            {
                UpdateShareState(file, EDiscoveryShareState.NotShared);
                await connection.File.PutAsync(file);
            }
        }
        
        public void TagSharePackage(FileModel fileModel, string pagackgeName)
        {
            fileModel.Write(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE, pagackgeName);
        }

        public void UpdateShareState(FileModel fileModel, EDiscoveryShareState state)
        {
            fileModel.Write(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY, state.ToString());
        }

        private async Task InitializePrivilegedConnectionAsync()
        {
            // Now we use the special priveliged connection to update users/recipients.
            await privilegedConnection.User.AuthenticateAsync(new TokenRequestModel
            {
                Identifier = new UserIdentifier
                {
                    OrganizationKey  = managerConfiguration.EDiscoveryImpersonationOrganization,
                    UserKey = managerConfiguration.EDiscoveryImpersonationUser
                },
                Password = managerConfiguration.EDiscoveryImpersonationPasssword
            });
        }

        #region Service Style Methods

        public async Task<EDiscoveryStatisticsResponse> GetEDiscoveryStatistics(FolderIdentifier folderIdentifier)
        {

            var stats = new EDiscoveryStatisticsResponse();

            // Setup our state so we have everything we need. 
            var folder = await connection.Folder.GetAsync(folderIdentifier, new List<PopulationDirective>
            {
                new PopulationDirective
                {
                    Name = nameof(FolderModel.Files)
                }
            });

            foreach (var file in folder.Files.Rows.ToList())
            {
                switch (EDiscoveryUtility.GetCurrentShareState(file))
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

            stats.RecipientCount = folder.MetaEDiscoveryRecipientListRead().Count();

            stats.IsEDiscoveryActive = this.IsModuleActive(folder);

            return stats;
        }

        public async Task<RecipientResponse> AddRecipientAsync(AddRecipientRequest addRecipientRequest, string landingLocation, string passphrase)
        {
            var folder = await connection.Folder.GetAsync(addRecipientRequest.FolderIdentifier);

            DateTime? expirationDate = ModuleUtility.GetLinkExpirationDate(folder, MetadataKeyConstants.E_DISCOVERY_EXPIRATION_LENGTH_SECONDS);

            var password = ModuleUtility.GeneratePassword(folder, MetadataKeyConstants.E_DISCOVERY_RND_PASSWORD_LENGTH, EDiscoveryUtility.E_DISCOVERY_DEFAULT_PASSWORD_LENGTH, MetadataKeyConstants.E_DISCOVERY_RND_PASSWORD_CHARS);

            // We're going to generate a user for eDicsovery.  This user will have restricted priveleges. 
            var user = await GenerateUserForEDiscovery(addRecipientRequest, password.Plain);

            string completeUrl = ModuleUtility.CreateMagicLink(addRecipientRequest, landingLocation, passphrase, folder.Identifier, expirationDate, user.Identifier);

            folder.MetaEDiscoveryRecipientListUpsert(new ExternalUser()
            {
                Email = addRecipientRequest.RecipientEmail,
                FirstName = addRecipientRequest.FirstName,
                LastName = addRecipientRequest.LastName,
                PasswordHash = password.Hashed,
                MagicLink = completeUrl,
                ExpirationDate = expirationDate.GetValueOrDefault()
            });
            await connection.Folder.PutAsync(folder);
            await EnsureFolderSecurityConfiguration(folder.Identifier);

            await this.auditLogStore.AddEntry(
                new AuditLogEntry()
                {
                    EntryType = AuditLogEntryType.eDiscoveryRecipientAdded,
                    Message = $"An eDiscovery User has been added. {addRecipientRequest.RecipientEmail}",
                    ModuleType = Modules.ModuleType.eDiscovery
                },
                folder.Identifier
            );

            // build up the response
            return new RecipientResponse()
            {
                Email = addRecipientRequest.RecipientEmail,
                ExpirationDate = expirationDate.GetValueOrDefault(),
                MagicLink = completeUrl,
                Password = password.Plain,
                FirstName = addRecipientRequest.FirstName,
                LastName = addRecipientRequest.LastName,
            };
        }

        public async Task<UserAuthenticatedResponse> AuthenticateUserAsync(AuthenticateUserRequest authenticateUserRequest, string eDiscoveryLinkEncryptionKey)
        {
            if (String.IsNullOrEmpty(authenticateUserRequest.Token))
            {
                return new UserAuthenticatedResponse() { IsAuthenticated = false };
            }

            // Get the magic link object back out from the token.
            var magicLink = ModuleUtility.DecryptMagicLink(authenticateUserRequest.Token, eDiscoveryLinkEncryptionKey);

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
                    Identifier = ModuleUtility.GetFolderScopedUserIdentifier(magicLink.FolderIdentifier, authenticateUserRequest.Email),
                    Password = authenticateUserRequest.Password
                });

                connection.AddCookieTokenToResponse();

                // Now we can do the password check.  We're going to check that the password in our database matches on hash 
                // the password that came in on the request.
                var folder = await connection.Folder.GetAsync(magicLink.FolderIdentifier);

                // get the recipients, so we can find this particular recipient.
                var recipients = folder.MetaEDiscoveryRecipientListRead();
                var recipient = recipients.Where(rec => rec.Email.ToLower() == magicLink.RecipientEmail.ToLower()).FirstOrDefault();

                // We check to make sure that there's a recipient on this folder that matches by email, and that the plain text
                // password that was passed in matches the password in our database. 
                if (recipient != null && BCrypt.Verify(authenticateUserRequest.Password, recipient.PasswordHash))
                {
                    await this.auditLogStore.AddEntry(
                            new AuditLogEntry()
                            {
                                EntryType = AuditLogEntryType.eDiscoveryUserLogin,
                                Message = "An eDiscovery User Has Logged in.",
                                ModuleType = Modules.ModuleType.eDiscovery,
                            },
                            folder.Identifier,
                            connection
                            );

                    return new UserAuthenticatedResponse()
                    {
                        IsAuthenticated = true,
                        FolderIdentifier = folder.Identifier
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

        public async Task<RecipientResponse> RemoveRecipientAsync(RemoveRecipientRequest removeRequest)
        {
            var folder = await connection.Folder.GetAsync(removeRequest.FolderIdentifier);
            var recipients = folder.MetaEDiscoveryRecipientListRead();

            folder.MetaEDiscoveryRecipientListRemove(removeRequest.RecipientEmail);

            await connection.Folder.PutAsync(folder);

            await InitializePrivilegedConnectionAsync();

            // Using the special connection here to delete the user. 
            await privilegedConnection.User.DeleteAsync(ModuleUtility.GetFolderScopedUserIdentifier(folder.Identifier, removeRequest.RecipientEmail));

            await this.auditLogStore.AddEntry(
                new AuditLogEntry()
                {
                    EntryType = AuditLogEntryType.eDiscoveryRecipientDeleted,
                    Message = $"An eDiscovery User has been removed {removeRequest.RecipientEmail}",
                    ModuleType = Modules.ModuleType.eDiscovery
                },
                folder.Identifier
            );

            return new RecipientResponse()
            {
                Email = removeRequest.RecipientEmail,
            };
        }

        public async Task<RecipientResponse> RegenerateRecipientPasswordAsync(RegenerateRecipientPasswordRequest regenRequest)
        {
            var folder = await connection.Folder.GetAsync(regenRequest.FolderIdentifier);
            var recipients = folder.MetaEDiscoveryRecipientListRead();

            var password = ModuleUtility.GeneratePassword(folder, MetadataKeyConstants.E_DISCOVERY_RND_PASSWORD_LENGTH, EDiscoveryUtility.E_DISCOVERY_DEFAULT_PASSWORD_LENGTH, MetadataKeyConstants.E_DISCOVERY_RND_PASSWORD_CHARS);

            var recipient = recipients.Where(rec => rec.Email.ToLower() == regenRequest.RecipientEmail.ToLower()).FirstOrDefault();
            if (recipient != null)
            {

                // Using the special connection here to update their password. 
                var userIdentifier = ModuleUtility.GetFolderScopedUserIdentifier(folder.Identifier, regenRequest.RecipientEmail);

                await InitializePrivilegedConnectionAsync();

                await privilegedConnection.User.PasswordPutAsync(userIdentifier, password.Plain);

                await this.auditLogStore.AddEntry(
                    new AuditLogEntry()
                    {
                        EntryType = AuditLogEntryType.eDiscoveryRecipientRegenerated,
                        Message = $"An eDiscovery has had their password regenerated {recipient.Email}",
                        ModuleType = Modules.ModuleType.eDiscovery
                    },
                    folder.Identifier
                );

                recipient.PasswordHash = password.Hashed;
                recipient.ExpirationDate = ModuleUtility.GetLinkExpirationDate(folder, MetadataKeyConstants.E_DISCOVERY_EXPIRATION_LENGTH_SECONDS).Value;
                recipient.MagicLink = ModuleUtility.CreateMagicLink(regenRequest, managerConfiguration.EDiscoveryLandingLocation, managerConfiguration.EDiscoveryLinkEncryptionKey, regenRequest.FolderIdentifier, recipient.ExpirationDate, userIdentifier);

                await connection.ConcurrencyRetryBlock(async () =>
                {
                    folder = await connection.Folder.GetAsync(regenRequest.FolderIdentifier);

                    folder.MetaEDiscoveryRecipientListUpsert(recipient);
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

        /// <summary>
        /// We need a way to generate users on the fly.  
        /// </summary>
        /// <param name="addRecipientRequest"></param>
        /// <returns></returns>
        private async Task<UserModel> GenerateUserForEDiscovery(AddRecipientRequest addRecipientRequest, string password)
        {
            await InitializePrivilegedConnectionAsync();

            var folderIdentifier = addRecipientRequest.FolderIdentifier;
            var userModel = new UserModel
            {
                Identifier = ModuleUtility.GetFolderScopedUserIdentifier(folderIdentifier, addRecipientRequest.RecipientEmail),
                EmailAddress = addRecipientRequest.RecipientEmail,
                FirstName = addRecipientRequest.FirstName,
                LastName = addRecipientRequest.LastName,
            };

            userModel = await privilegedConnection.User.PostAsync(userModel);
            await privilegedConnection.User.PasswordPutAsync(userModel.Identifier, password);

            var accessIdentifiers = new[]
            {
                $"o:{folderIdentifier.OrganizationKey}",
                $"r:eDiscovery{{{folderIdentifier.FolderKey.Replace(" ", "_")}}}", // used to actually control access
                $"r:eDiscovery", // used to test whether a given user is an eDiscovery user
                $"x:pcms", // disable PCMS rules
                $"x:leo", // disable LEO rules
            };

            await privilegedConnection.User.AccessIdentifiersPutAsync(userModel.Identifier, accessIdentifiers);

            return userModel;
        }

        private async Task EnsureFolderSecurityConfiguration(FolderIdentifier folderIdentifier)
        {
            await privilegedConnection.ConcurrencyRetryBlock(async () =>
            {
                var folder = await privilegedConnection.Folder.GetAsync(folderIdentifier);
                if (!folder.Privilege("read")?.Any(a => a.OverrideKey == "edisc") ?? false)
                {
                    folder.WriteACLs("read", new[] {
                        new ACLModel
                        {
                            OverrideKey = "edisc",
                            RequiredIdentifiers = new List<string>
                            {
                                "u:system",
                                "x:eDiscovery",
                                $"r:eDiscovery{{{folderIdentifier.FolderKey.Replace(" ", "_")}}}"
                            }
                        }
                    });

                    await privilegedConnection.Folder.PutAsync(folder);
                }

            });

        }

        #endregion
    }
}