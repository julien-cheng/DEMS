namespace Documents.Clients.Manager.Modules.ItemQueryHandlers.LEOUpload
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class LEOUploadRootQueryHandler : BaseItemQueryHandler
    {
        private readonly IAuditLogStore auditLogStore;
        private readonly LEOUploadModule leoUploadModule;
        private readonly ManagerConfiguration ManagerConfiguration;

        public LEOUploadRootQueryHandler(PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, LEOUploadModule leoUploadModule, ManagerConfiguration managerConfiguration, FileService fileService) : base(pathService, connection, managerConfiguration, fileService)
        {
            this.auditLogStore = auditLogStore;
            this.leoUploadModule = leoUploadModule;
            this.ManagerConfiguration = managerConfiguration;
        }

        protected override List<AllowedOperation> GetPageAllowedOperations(PathIdentifier identifier)
        {
            var ops = new List<AllowedOperation>();

            if (ManagerConfiguration.IsFeatureEnabledSearch)
                ops.Add(AllowedOperation.GetAllowedOperationSearch(identifier));

            return ops;
        }

        protected override void FilterFiles(PathIdentifier identifier)
        {
            // We shouldn't be returning a list of any files in the leo Upload folder.
            filteredFiles = new List<FileModel>();
        }

        private static List<AllowedOperation> GetAllowedOperationsForRecipient(FolderIdentifier folderIdentifier, ExternalUser recipient)
        {
            return new List<AllowedOperation>() {
                AllowedOperation.GetAllowedOperationRegenOfficerPassword(folderIdentifier, recipient.Email),
                AllowedOperation.GetAllowedOperationRemoveOfficer(folderIdentifier, recipient.Email),
            };
        }


        protected async override Task BuildViews(List<IModule> activeModules, int pageIndex, int pageSize, FolderModel folder, PathIdentifier identifier, string userTimeZone)
        {
            this.page.Views = new List<Models.Responses.IViewModel>();

            // This will show the instructions on how to work with EDiscovery
            var instructions = new DataViewModel
            {
                DataModel = null,
                DataSchema = new ManagerFieldObject()
                {
                    IsCollapsed = false,
                    Properties = new Dictionary<string, ManagerFieldBaseSchema>() {
                            { "Instructions", new ManagerFieldNull() {
                                Description = @"<p>
                                                    Law Enforcement Officers can add files and folders directly to your case.
                                                </p><p>
                                                    Below are officers that you have authorized to contribute to this case. 
                                                    To add someone new, click ""Add Officer"", fill out the form, and email
                                                    the provided access information.  Click the action button next to an officer
                                                    to regenerate their access link/password or to remove them from the case. 
                                                </p><p>
                                                    Uploads will be added to a folder with the officer's name.  
                                                    Files can be moved from an officer folder into other folders within the case.
                                                    Once moved, the officer will no longer be able to see the files they added.
                                                </p>",
                                IsReadOnly = true,
                                Order = 0,
                                Title = "LEO Upload Instructions"
                            }
                        },
                    }
                },
                AllowedOperations = null
            };

            this.page.Views.Add(instructions);

            this.page.Views.Add(RecipientViewBuilder.BuildPagedGridView(
                pageIndex,
                pageSize,
                folder.MetaLEOUploadOfficerListRead(),
                folder.Identifier,
                userTimeZone,
                GridViewModel.GRID_TITLES_LEO_UPLOAD_OFFICERS,
                new List<AllowedOperation>() { AllowedOperation.GetAllowedOperationAddOfficer(folder.Identifier) },
                GetAllowedOperationsForRecipient
            ));

            // We need to take a list of manager path models, and convert them to a list of item query response object
            //TODO Add in our child officer folders.
            //var managerPathModels = this.LEOUploadModule.BuildDatedPackagesDynamicFolder(folder);
            //managerPathModels.Add(this.LEOUploadModule.GetNotSharedYetPath(folder));

            // Now we need to convert these manager path models into something that's a list of 
            var dynamicPaths = new List<IItemQueryResponse>();

            //TODO Add in our child officer folders.
            //dynamicPaths.AddRange(managerPathModels);

            //this.page.Views.Add(
            //    GridViewBuilder.BuildGridView(
            //        pageIndex,
            //        pageSize,
            //        dynamicPaths,
            //        new List<GridColumnSpecification>() { GridColumnSpecification.GetNameColumn(), GridColumnSpecification.GetCustomNameColumn(), GridColumnSpecification.GetActionsColumn() },
            //        GridViewModel.GRID_TITLES_EDISOVERY_PACKAGES)
            //    );

            // Now we build up the list of audit log entries.
            var auditLogEntries = this.auditLogStore.TranslateEntriesForDisplay(
                    await this.auditLogStore.GetEntries(folder.Identifier, ModuleType.LEOUpload), userTimeZone
                );

            this.page.Views.Add(AuditLogViewBuilder.BuildPagedGridView(pageIndex, pageSize, auditLogEntries, folder.Identifier, i => (i as AuditLogEntry)?.Created));
        }
    }
}
