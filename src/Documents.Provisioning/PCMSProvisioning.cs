namespace Documents.Provisioning
{
    using Documents.API.Client;
    using Documents.API.Common.EventHooks;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Security;
    using Documents.Provisioning.Models;
    using Documents.Queues.Messages;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class PCMSProvisioning
    {
        private const int AES_KEY_SIZE_BYTES = 32;
        private readonly Connection api;

        private string idSystem;
        private string idOrganizationMember;
        private string idOrganizationPCMSBridge;
        private string idDeleteDocuments;

        public PCMSProvisioning(Connection api)
        {
            this.api = api;
        }

        public async Task<OrganizationModel> OrganizationApply(PCMSOrganizationModel pcms)
        {
            EnsureValidConfigurationBasic(pcms);

            var isOrganizationNew = false;

            // create the organization
            var organizationIdentifier = CreateOrganizationIdentifier(pcms);
            var organizationModel = await api.Organization.GetAsync(organizationIdentifier);

            if (organizationModel == null)
            {
                isOrganizationNew = true;
                EnsureValidConfigurationIfCreating(pcms);

                organizationModel = new OrganizationModel(organizationIdentifier)
                {
                    Name = pcms.CountyName
                }
                    .InitializeEmptyMetadata()
                    .InitializeEmptyPrivileges();
            }

            organizationModel.Write("type", "pcms");

            // define user access identifiers
            idSystem = "u:system";
            idOrganizationMember = $"o:{organizationIdentifier.OrganizationKey}";
            idOrganizationPCMSBridge = $"o:{organizationIdentifier.OrganizationKey}:bridge";
            idDeleteDocuments = $"g:DocumentsDeleteGenerated";

            organizationModel.WriteACLs("read", idSystem, idOrganizationMember);
            organizationModel.WriteACLs("write", idSystem, idOrganizationPCMSBridge);
            organizationModel.WriteACLs("delete", idSystem);

            organizationModel.WriteACLs("folder:create", idSystem, idOrganizationMember);

            organizationModel.WriteACLs("user:create", idSystem, idOrganizationPCMSBridge);
            organizationModel.WriteACLs("user:read", idSystem, idOrganizationMember);
            organizationModel.WriteACLs("user:write", idSystem, idOrganizationPCMSBridge);
            organizationModel.WriteACLs("user:delete", idSystem, idOrganizationPCMSBridge);
            organizationModel.WriteACLs("user:credentials", idSystem, idOrganizationPCMSBridge);
            organizationModel.WriteACLs("user:identifiers", idSystem, idOrganizationPCMSBridge);
            organizationModel.WriteACLs("user:impersonate", idSystem, idOrganizationPCMSBridge);

            organizationModel.WriteACLsForFolder("create", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFolder("read", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFolder("write", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFolder("delete", idSystem, idDeleteDocuments);
            organizationModel.WriteACLsForFolder("file:create", idSystem, idOrganizationMember);

            organizationModel.WriteACLsForFile("read", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFile("write", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFile("delete", idSystem, idDeleteDocuments);

            this.ConfigureEDiscoveryAndLEO(organizationModel, pcms);
            this.ConfigureEvents(organizationModel, pcms);
            this.ConfigureSearch(organizationModel);

            await api.Organization.PutAsync(organizationModel);

            await this.ConfigureSuggestedPathsFolder(organizationModel, pcms);

            // only write the BackendConfiguration when the organization is
            // new. This prevents changing buckets with this tool, but
            // it also prevents overwriting master keys
            if (isOrganizationNew || pcms.OverrideBackendConfiguration)
            {
                await this.ConfigureBackendAsync(organizationModel, pcms);
            }

            if (isOrganizationNew || pcms.PCMSBridgeUserPassword != null)
                await this.ConfigureBridgeUserAsync(organizationModel, pcms);

            await this.ConfigureVideoWatermarkingAsync(organizationModel);
            await this.ConfigureNotificationTemplatesAsync(organizationModel);

            return organizationModel;
        }

        private async Task ConfigureNotificationTemplatesAsync(OrganizationModel organizationModel)
        {
            var folderIdentifier = new FolderIdentifier(organizationModel.Identifier, ":templates");

            var folder = await api.Folder.GetAsync(folderIdentifier);

            if (folder == null)
                folder = await api.Folder.PutAsync(new FolderModel(folderIdentifier));

            var assembly = typeof(PCMSProvisioning).Assembly;
            FileModel file = null;
            FileIdentifier fileIdentifier = null;

            fileIdentifier = new FileIdentifier(folder.Identifier, "upload.body.mustache");
            file = await api.File.GetAsync(fileIdentifier);

            if (file == null)
            {
                using (var stream = assembly.GetManifestResourceStream("Documents.Provisioning.Resources.templates.upload.body.mustache"))
                    await api.File.UploadAsync(
                        new FileModel(fileIdentifier)
                        {
                            MimeType = "text/plain",
                            Name = "upload.body.mustache",
                            Length = stream.Length
                        },
                        stream
                    );
            }

            fileIdentifier = new FileIdentifier(folder.Identifier, "upload.subject.mustache");
            file = await api.File.GetAsync(fileIdentifier);
            if (file == null)
            {
                using (var stream = assembly.GetManifestResourceStream("Documents.Provisioning.Resources.templates.upload.subject.mustache"))
                    await api.File.UploadAsync(
                        new FileModel(fileIdentifier)
                        {
                            MimeType = "text/plain",
                            Name = "upload.subject.mustache",
                            Length = stream.Length
                        },
                        stream
                    );
            }
        }

        private async Task ConfigureVideoWatermarkingAsync(OrganizationModel organizationModel)
        {
            var folderIdentifier = new FolderIdentifier(organizationModel.Identifier, ":watermarks");

            var folder = await api.Folder.GetAsync(folderIdentifier);

            if (folder == null)
                folder = await api.Folder.PutAsync(new FolderModel(folderIdentifier));

            var fileIdentifier = new FileIdentifier(folder.Identifier, "video.png");
            var file = await api.File.GetAsync(fileIdentifier);

            if (file == null)
            {
                var assembly = typeof(PCMSProvisioning).Assembly;
                using (var stream = assembly.GetManifestResourceStream("Documents.Provisioning.Resources.watermarks.video.png"))
                    await api.File.UploadAsync(
                        new FileModel(fileIdentifier)
                        {
                            MimeType = "image/png",
                            Name = "video.png",
                            Length = stream.Length
                        },
                        stream
                    );
            }
        }

        private void ConfigureSearch(OrganizationModel organizationModel)
        {
            organizationModel.Write("searchconfiguration", new
            {
                languageMap = new Dictionary<string, string> {
                    { "_path", "Path" },
                    { "attribute.make", "Camera Make" },
                    { "attribute.model", "Camera Model" },
                    { "attribute.casestatus", "Case Status" },
                    { "attribute.closed", "Open or Closed" },
                    { "attribute.closed.true", "Closed" },
                    { "attribute.closed.false", "Open" },
                    { "attribute.ada.last", "Primary ADA" },
                    { "attribute.firstname", "Defendant First name" },
                    { "attribute.lastname", "Defendant Last name" }
                },
                displayFields = new string[]
                {
                    "attribute.firstname",
                    "attribute.lastname",
                    "attribute.casestatus"
                }
            });

            organizationModel.WriteForFolder("attributelocators[locatorlist]", new List<AttributeLocator>()
            {
                new AttributeLocator {
                    Key = "_path",
                    Label = "Path",
                    IsFacet = true,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = false,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.defendantid",
                    Label = "Defendant ID",
                    IsFacet = false,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = false,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.ada.last",
                    Label = "ADA Last Name",
                    IsFacet = true,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = false,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.firstname",
                    Label = "First Name",
                    IsFacet = false,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = false,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.lastname",
                    Label = "Last Name",
                    IsFacet = true,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = false,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.casestatus",
                    Label = "Case Status",
                    IsFacet = true,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.closed",
                    Label = "Closed",
                    IsFacet = true,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = false,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.deleted",
                    Label = "Deleted",
                    IsFacet = false,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = false,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.created",
                    Label = "Created",
                    IsFacet = false,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.make",
                    Label = "Camera Make",
                    IsFacet = true,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.model",
                    Label = "Camera Model",
                    IsFacet = true,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.gps",
                    Label = "GPS Coordinates",
                    IsFacet = false,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.datetimeoriginal",
                    Label = "Date/Time Original",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.height",
                    Label = "Height",
                    IsFacet = false,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.width",
                    Label = "Width",
                    IsFacet = false,
                    IsIndexed = true,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.lensid",
                    Label = "Lens ID",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.software",
                    Label = "Software",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.filesize",
                    Label = "EXIF File Size",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.duration",
                    Label = "Duration in Seconds",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.isosetting",
                    Label = "ISO Setting",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.shutterspeed",
                    Label = "Shutter Speed",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.fnumber",
                    Label = "F Number",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.flash",
                    Label = "Flash",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.megapixels",
                    Label = "Megapixels",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },
                new AttributeLocator {
                    Key = "attribute.imageuniqueid",
                    Label = "Image Unique ID",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },

                new AttributeLocator {
                    Key = "attribute.codec",
                    Label = "Codec",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },

                new AttributeLocator {
                    Key = "attribute.framerate",
                    Label = "Frame Rate",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },

                new AttributeLocator {
                    Key = "attribute.framecount",
                    Label = "Frame Count",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                },

                new AttributeLocator {
                    Key = "attribute.resolution",
                    Label = "Resolution",
                    IsFacet = false,
                    IsIndexed = false,
                    IsReadOnly = true,
                    IsOnDetailView = true,
                    StorageType = StorageType.SystemString
                }
            });
        }

        private async Task ConfigureBridgeUserAsync(OrganizationModel organizationModel, PCMSOrganizationModel pcms)
        {
            // create a user for the PCMS Documents Bridge
            var user = await api.User.PutAsync(new UserModel
            {
                Identifier = new UserIdentifier(
                    organizationModel.Identifier,
                    $"{organizationModel.Identifier.OrganizationKey}:Bridge"
                ),
                EmailAddress = "support@nypti.org",
                FirstName = "PCMS",
                LastName = "Bridge"
            });

            await api.User.AccessIdentifiersPutAsync(user.Identifier, new[] { idOrganizationMember, idOrganizationPCMSBridge, "x:pcms" });
            await api.User.PasswordPutAsync(user.Identifier, pcms.PCMSBridgeUserPassword);
        }

        private async Task ConfigureSuggestedPathsFolder(OrganizationModel organizationModel, PCMSOrganizationModel pcms)
        {
            var suggestedPathsFolderIdentifier = new FolderIdentifier(organizationModel.Identifier, ":suggestedpaths");

            var suggestedPathsFolder = await api.Folder.GetAsync(suggestedPathsFolderIdentifier)
                ?? new FolderModel(suggestedPathsFolderIdentifier)
                    .InitializeEmptyMetadata()
                    .InitializeEmptyPrivileges();

            if (suggestedPathsFolder.Read<List<string>>("_paths") == null)
            {
                suggestedPathsFolder.Write("_paths", new string[0]);
                await api.Folder.PutAsync(suggestedPathsFolder);
            }
        }

        private async Task ConfigureBackendAsync(OrganizationModel organizationModel, PCMSOrganizationModel pcms)
        {
            // create a private folder to store backend configuration
            var privateFolder = new FolderModel(new FolderIdentifier(organizationModel.Identifier, ":private"))
                .InitializeEmptyMetadata()
                .InitializeEmptyPrivileges();

            // write the backend configuration into the folder's metadata
            var backendConfiguration = new BackendConfiguration
            {
                DriverTypeName = "Documents.Backends.Drivers.Encryption.Driver, Documents.Backends.Drivers.Encryption",
                ConfigurationJSON = JsonConvert.SerializeObject(new
                {
                    NextDriverTypeName = "Documents.Backends.Drivers.S3.Driver, Documents.Backends.Drivers.S3",
                    MasterKey = pcms.MasterEncryptionKey,
                    NextDriverConfigurationJson = JsonConvert.SerializeObject(new
                    {
                        AWSRegion = pcms.AWSS3Region,
                        BucketName = pcms.AWSS3BucketName,
                        pcms.AWSSecretAccessKey,
                        pcms.AWSAccessKeyID
                    })
                })
            };
            privateFolder.Write(MetadataKeyConstants.BACKEND_CONFIGURATION, backendConfiguration);
            privateFolder.WriteACLs("read", idSystem);
            privateFolder.WriteACLs("write", idSystem);
            privateFolder.WriteACLs("gateway", idSystem, idOrganizationMember);

            privateFolder.Write("synchronize", new
            {
                ConnectionString = pcms.PCMSDBConnectionString,
                pcms.CountyID,
                LastChangeLogID = 0,
                LastAccountChangeLogID = 0
            });

            if (organizationModel.Read<bool?>("synchronize[isactive]") == null)
                organizationModel.Write("synchronize[isactive]", true);

            await api.Folder.PutAsync(privateFolder);
        }

        public async Task UpgradeFoldersAsync(OrganizationIdentifier organizationIdentifier)
        {
            await api.ConcurrencyRetryBlock(async () =>
            {
                var organization = await api.Organization.GetAsync(organizationIdentifier, new[]
                {
                    new PopulationDirective(nameof(OrganizationModel.Folders))
                });

                foreach (var folder in organization.Folders.Rows)
                {
                    var dirty = false;

                    bool isEDiscovery = folder.Read<object>("ediscovery[recipients]") != null
                        || folder.Read<object>("ediscovery[packagemap]") != null;

                    if (isEDiscovery)
                    {
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
                                        $"r:eDiscovery{{{folder.Identifier.FolderKey}}}"
                                    }
                                }
                            });

                            dirty = true;
                        }
                    }

                    if (dirty)
                        await api.Folder.PutAsync(folder);
                }
            });
        }


        public async Task UpgradeUsersAsync(OrganizationIdentifier organizationIdentifier)
        {
            await api.ConcurrencyRetryBlock(async () =>
            {
                var organization = await api.Organization.GetAsync(organizationIdentifier, new[]
                {
                    new PopulationDirective(nameof(OrganizationModel.Users))
                });

                foreach (var user in organization.Users.Rows)
                {
                    var dirty = false;
                    var identifiers = user.UserAccessIdentifiers.ToList();

                    if (user.Identifier.UserKey == $"{organizationIdentifier.OrganizationKey}:Bridge")
                    {
                        if (!user.UserAccessIdentifiers.Any(i => i == "x:pcms"))
                        {
                            identifiers.Add(
                                "x:pcms"
                            );
                            dirty = true;
                        }
                    }

                    bool isEDiscoveryUser = identifiers.Any(u => u == "r:eDiscovery");
                    // if this is an eDiscovery recipient
                    if (isEDiscoveryUser)
                    {
                        var isFolderSpecific = identifiers.Any(u => u.StartsWith("r:eDiscovery{"));
                        if (!isFolderSpecific)
                        {
                            var matches = Regex.Match(user.Identifier.UserKey, @"^(Defendant:\d*):");
                            var folderKey = matches.Groups[1].Value;

                            identifiers.Add(
                                $"r:eDiscovery{{{folderKey}}}" // yields r:eDiscovery{value}
                            );

                            dirty = true;
                        }

                        // exempt this ediscovery recipient from pcms rules
                        if (!user.UserAccessIdentifiers.Any(i => i == "x:pcms"))
                        {
                            identifiers.Add(
                                "x:pcms"
                            );
                            dirty = true;
                        }
                    }
                    else
                    {
                        // exempt this pcms user from ediscovery rules
                        if (!user.UserAccessIdentifiers.Any(i => i == "x:eDiscovery"))
                        {
                            identifiers.Add(
                                "x:eDiscovery"
                            );
                            dirty = true;
                        }
                    }

                    bool isLEOUser = identifiers.Any(u => u == "r:leoUpload");
                    if (!isLEOUser)
                    {
                        if (!identifiers.Any(u => u == "x:leo"))
                        {
                            identifiers.Add(
                                "x:leo"
                            );
                            dirty = true;
                        }
                    }

                    if (dirty)
                    {
                        await api.User.PutAsync(user);
                        var updatedUser = await api.User.AccessIdentifiersPutAsync(user.Identifier, identifiers);
                    }
                }
            });
        }

        public async Task<bool> OrganizationExists(PCMSOrganizationModel pcms)
        {
            var organizationModel = await api.Organization.GetAsync(CreateOrganizationIdentifier(pcms));
            return organizationModel != null;
        }

        private OrganizationIdentifier CreateOrganizationIdentifier(PCMSOrganizationModel pcms)
        {
            return new OrganizationIdentifier(pcms.UseOrganizationKey ?? $"PCMS:{pcms.CountyID}");
        }

        private void ConfigureEDiscoveryAndLEO(OrganizationModel organizationModel, PCMSOrganizationModel pcms)
        {
            if (pcms.EDiscoveryActive != null)
                organizationModel.WriteForFolder("eDiscovery[isactive]", pcms.EDiscoveryActive);
            if (pcms.LEOActive != null)
                organizationModel.WriteForFolder("LEOUpload[isactive]", pcms.LEOActive);
        }

        private void ConfigureEvents(OrganizationModel organizationModel, PCMSOrganizationModel pcms)
        {
            organizationModel.WriteForFolder("_events", new List<EventQueueMapBase> {
                new EventQueueManager()
            }, withTypeName: true);

            organizationModel.WriteForFolder("_imagegen[options]", new List<ImageGenMessage>
            {
                new ImageGenMessage
                {
                    Format = ImageFormatEnum.PNG,
                    AlternativeViewSizeType = "Thumbnail",
                    MaxHeight = 100
                }
            });
        }

        private void EnsureValidConfigurationIfCreating(PCMSOrganizationModel pcms)
        {
            if (pcms.MasterEncryptionKey == null)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.MasterEncryptionKey));
            if (Convert.FromBase64String(pcms.MasterEncryptionKey).Length != AES_KEY_SIZE_BYTES)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.MasterEncryptionKey));
            if (pcms.PCMSBridgeUserPassword == null)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.PCMSBridgeUserPassword));
            if (pcms.AWSAccessKeyID == null)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.AWSAccessKeyID));
            if (pcms.AWSSecretAccessKey == null)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.AWSSecretAccessKey));
            if (pcms.AWSS3BucketName == null)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.AWSS3BucketName));
            if (pcms.AWSS3Region == null)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.AWSS3Region));

        }

        private void EnsureValidConfigurationBasic(PCMSOrganizationModel pcms)
        {
            // validate configuration
            if (pcms == null)
                throw new ArgumentNullException(nameof(pcms));

            if (pcms.CountyID == 0)
                throw new ArgumentNullException(nameof(PCMSOrganizationModel.CountyID));
        }

        public string GeneratePCMSBridgeUserPassword()
        {
            return PasswordGenerator.GetRandomString();
        }
        public string GenerateMasterEncryptionKey()
        {
            return Convert.ToBase64String(PasswordGenerator.GetRandomBytes(AES_KEY_SIZE_BYTES));
        }
    }
}
