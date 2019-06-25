namespace Documents.Clients.Manager.Models
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models.eArraignment;
    using Documents.Clients.Manager.Models.LEOUpload.Requests;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.Requests.eDiscovery;
    using System.Linq;

    public class AllowedOperation : ModelBase
    {
        public string DisplayName { get; set; }
        public string[] Icons { get; set; }
        public ModelBase BatchOperation { get; set; }
        public bool IsSingleton { get; set; }

        #region LEO
        public static AllowedOperation GetAllowedOperationRegenOfficerPassword(FolderIdentifier folderIdentifier, string email)
        {
            return new AllowedOperation()
            {
                DisplayName = "Regenerate Access Link/Password",
                IsSingleton = true,
                BatchOperation = new RegenerateOfficerRequest()
                {
                    FolderIdentifier = folderIdentifier,
                    RecipientEmail = email
                }
            };
        }

        public static AllowedOperation GetAllowedOperationRemoveOfficer(FolderIdentifier folderIdentifier, string email)
        {
            return new AllowedOperation()
            {
                DisplayName = "Remove Officer",
                BatchOperation = new RemoveOfficerRequest()
                {
                    FolderIdentifier = folderIdentifier,
                    RecipientEmail = email,
                }
            };
        }
        #endregion

        #region eDiscovery
        public static AllowedOperation GetAllowedOperationRegenPassword(FolderIdentifier folderIdentifier, string email)
        {
            return new AllowedOperation()
            {
                DisplayName = "Regenerate Access Link/Password",
                IsSingleton = true,
                BatchOperation = new RegenerateRecipientPasswordRequest()
                {
                    FolderIdentifier = folderIdentifier,
                    RecipientEmail = email
                }
            };
        }

        public static AllowedOperation GetAllowedOperationEditPackageName(FolderIdentifier folderIdentifier, string packageName)
        {
            return new AllowedOperation()
            {
                DisplayName = "Edit Description",
                IsSingleton = true,
                BatchOperation = new EditPackageNameRequest()
                {
                    FolderIdentifier = folderIdentifier,
                    PackageName = packageName,
                }
            };
        }

        public static AllowedOperation GetAllowedOperationRemoveRecipient(FolderIdentifier folderIdentifier, string email)
        {
            return new AllowedOperation()
            {
                DisplayName = "Remove Recipient",
                BatchOperation = new RemoveRecipientRequest()
                {
                    FolderIdentifier = folderIdentifier,
                    RecipientEmail = email,
                }
            };
        }

        public static AllowedOperation GetAllowedOperationShare(FileIdentifier fileIdentifier, bool isShared)
        {
            return new AllowedOperation()
            {
                DisplayName = isShared ? "To Share" : "Unshare",
                BatchOperation = isShared
                    ? new ShareRequest() { FileIdentifier = fileIdentifier } as ModelBase
                    : new UnshareRequest() { FileIdentifier = fileIdentifier } as ModelBase
            };
        }
        public static AllowedOperation GetAllowedOperationShare(PathIdentifier pathIdentifier, bool isShared)
        {
            return new AllowedOperation()
            {
                DisplayName = isShared ? "To Share" : "Unshare",
                BatchOperation = isShared
                    ? new ShareRequest() { PathIdentifier = pathIdentifier } as ModelBase
                    : new UnshareRequest() { PathIdentifier = pathIdentifier } as ModelBase
            };
        }

        public static AllowedOperation GetAllowedOperationSendToEArraignment(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation()
            {
                DisplayName = "Send to eArraignment",
                BatchOperation = new SendToEArraignmentRequest()
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationPublish(FolderIdentifier folderIdentifier, int recipientCount)
        {
            return new AllowedOperation()
            {
                DisplayName = "Turn Over",
                BatchOperation = new PublishRequest()
                {
                    FolderIdentifier = folderIdentifier,
                    eDiscoveryRecipientCount = recipientCount,
                }
            };
        }

        #endregion

        public static AllowedOperation GetAllowedOperationSearch(FolderIdentifier folderIdentifier)
        {
            return new AllowedOperation()
            {
                DisplayName = "Search",
                IsSingleton = true,
                BatchOperation = new Requests.SearchRequest()
                {
                    FolderIdentifier = folderIdentifier,
                }
            };
        }

        public static AllowedOperation GetAllowedOperationExtractZip(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Extract Zip",
                BatchOperation = new ExtractRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationDownloadZip(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Download Zip",
                BatchOperation = new DownloadZipFileRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationRequestOnlineFolder(FolderIdentifier folderIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Restore Case",
                BatchOperation = new RequestOnlineRequest
                {
                    FolderIdentifier = folderIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationDownloadZip(PathIdentifier pathIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Download Zip",
                BatchOperation = new DownloadZipFileRequest
                {
                    PathIdentifier = pathIdentifier
                }
            };
        }

        internal static AllowedOperation GetAllowedOperationAddRecipient(FolderIdentifier folderIdentifier, FolderModel folder)
        {

            var recipients = folder.MetaEDiscoveryRecipientListRead();

            var defenseEmail = folder.Read<string>("attribute.defense.email");
            AddRecipientRequest.AddRecipientDefaults defaults =
                defenseEmail == null || recipients.Any(r => r.Email == defenseEmail)
                ? new AddRecipientRequest.AddRecipientDefaults()
                : new AddRecipientRequest.AddRecipientDefaults
                {
                    FirstName = folder.Read<string>("attribute.defense.first"),
                    LastName = folder.Read<string>("attribute.defense.last"),
                    Email = folder.Read<string>("attribute.defense.email")
                };

            return new AllowedOperation
            {
                DisplayName = "Add Recipient",
                IsSingleton = true,
                BatchOperation = new AddRecipientRequest
                {
                    FolderIdentifier = folderIdentifier,
                    Defaults = defaults
                }
            };
        }

        internal static AllowedOperation GetAllowedOperationAddOfficer(FolderIdentifier folderIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Add Officer",
                IsSingleton = true,
                BatchOperation = new AddOfficerRequest
                {
                    FolderIdentifier = folderIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationWatermarkVideo(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Create Watermarked Copy",
                Icons = new[] { "fa-tint" },
                BatchOperation = new WatermarkVideoRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationExportFrame(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Export Frame",
                BatchOperation = new ExportFrameRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationExportClip(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Export Clip",
                BatchOperation = new ExportClipRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationTranscribe(FileIdentifier fileIdentifier, string label = "Order Transcript")
        {
            return new AllowedOperation
            {
                DisplayName = label,
                Icons = new[] { "fa-assistive-listening-systems" },
                BatchOperation = new TranscriptionRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationDownload(FileIdentifier fileIdentifier, bool open = false, string label = "Download")
        {
            return new AllowedOperation
            {
                DisplayName = label,
                BatchOperation = new DownloadRequest
                {
                    FileIdentifier = fileIdentifier,
                    Open = open
                },
                IsSingleton = true,
            };
        }

        public static AllowedOperation GetAllowedOperationSave(FolderIdentifier folderIdentifier = null, FileIdentifier fileIdentifier = null)
        {
            return new AllowedOperation()
            {
                IsSingleton = true,
                BatchOperation = new SaveRequest() { FolderIdentifier = folderIdentifier, FileIdentifier = fileIdentifier  },
                DisplayName = "Save",
                Icons = new string[] { "save" },
            };
        }

        public static AllowedOperation GetAllowedOperationRename(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Rename",
                IsSingleton = true,
                BatchOperation = new RenameRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationRename(PathIdentifier pathIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Rename",
                IsSingleton = true,
                BatchOperation = new RenameRequest
                {
                    PathIdentifier = pathIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationMove(PathIdentifier sourcePathIdentifier, PathIdentifier targetPathIdentifier = null)
        {
            return new AllowedOperation
            {
                DisplayName = "Move",
                BatchOperation = new MoveIntoRequest
                {
                    SourcePathIdentifier = sourcePathIdentifier,
                    TargetPathIdentifier = targetPathIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationMove(FileIdentifier sourceFileIdentifier, PathIdentifier targetPathIdentifier = null)
        {
            return new AllowedOperation
            {
                DisplayName = "Move",
                BatchOperation = new MoveIntoRequest
                {
                    SourceFileIdentifier = sourceFileIdentifier,
                    TargetPathIdentifier = targetPathIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationDelete(PathIdentifier pathIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Delete",
                BatchOperation = new DeleteRequest
                {
                    PathIdentifier = pathIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationDelete(FileIdentifier fileIdentifier)
        {
            return new AllowedOperation
            {
                DisplayName = "Delete",
                BatchOperation = new DeleteRequest
                {
                    FileIdentifier = fileIdentifier
                }
            };
        }

        public static AllowedOperation GetAllowedOperationUpload(PathIdentifier pathIdentifier)
        {
            return new AllowedOperation
            {
                BatchOperation = new UploadRequest
                {
                    PathIdentifier = pathIdentifier
                },
                DisplayName = "Upload",
                Icons = new[] { "upload" }
            };
        }

        public static AllowedOperation GetAllowedOperationNewPath(PathIdentifier pathIdentifier)
        {
            return new AllowedOperation
            {
                BatchOperation = new NewPathRequest
                {
                    PathIdentifier = pathIdentifier
                },
                DisplayName = "New",
                Icons = new[] { "folder yellow" }
            };
        }
    }
}
