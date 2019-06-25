namespace Documents.Clients.Manager.Modules.ItemQueryHandlers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class EDiscoveryRootQueryHandler : BaseItemQueryHandler
    {
        private readonly IAuditLogStore auditLogStore;
        private readonly EDiscovery eDiscovery;
        private readonly ManagerConfiguration ManagerConfiguration;

        public EDiscoveryRootQueryHandler(PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, EDiscovery eDiscovery, ManagerConfiguration managerConfiguration, FileService fileService) : base(pathService, connection, managerConfiguration, fileService)
        {
            this.auditLogStore = auditLogStore;
            this.eDiscovery = eDiscovery;
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
            // We shouldn't be returning a list of any files in the ediscovery folder.
            filteredFiles = new List<FileModel>();
        }

        private static List<AllowedOperation> GetAllowedOperationsForRecipient(FolderIdentifier folderIdentifier, ExternalUser recipient)
        {
            return new List<AllowedOperation>() {
                AllowedOperation.GetAllowedOperationRegenPassword(folderIdentifier, recipient.Email),
                AllowedOperation.GetAllowedOperationRemoveRecipient(folderIdentifier, recipient.Email),
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
                                                  Files/folders that you have marked for sharing will appear in the ""Not Yet Shared"" folder and will remain there until you click <b>""Turn Over.""</b>
                                                </p>
                                                <p>
                                                  Clicking <b>""Turn Over""</b> will move the files from ""Not Yet Shared"" into a Discovery Package of files.
                                                </p>
                                                <p>
                                                  Once turned over, files cannot be edited or deleted.  Files turned over will still be in their original file folders, but the color will change to green.
                                                </p>
                                                <p>
                                                  Below are other users that you have authorized to view Discovery Packages.  To add someone new, click ""Add Recipient"" and fill out the form.  Click the action button next to a user to regenerate their access link/password or to remove them from the case.
                                                </p>",
                                IsReadOnly = true,
                                Order = 0,
                                Title = "eDiscovery Instructions"
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
                folder.MetaEDiscoveryRecipientListRead(), 
                folder.Identifier, 
                userTimeZone, 
                GridViewModel.GRID_TITLES_EDISOVERY_RECIPIENTS,
                new List<AllowedOperation>() { AllowedOperation.GetAllowedOperationAddRecipient(folder.Identifier, folder) },
                GetAllowedOperationsForRecipient
            ));

            // We need to take a list of manager path models, and convert them to a list of item query response object
            var managerPathModels = this.eDiscovery.BuildDatedPackagesDynamicFolder(folder);
            managerPathModels.Add(this.eDiscovery.GetNotSharedYetPath(folder));

            // Now we need to convert these manager path models into something that's a list of 
            var dynamicPaths = new List<IItemQueryResponse>();

            dynamicPaths.AddRange(managerPathModels);

            this.page.Views.Add(
                GridViewBuilder.BuildGridView(
                    pageIndex,
                    pageSize,
                    dynamicPaths,
                    new List<GridColumnSpecification>() { GridColumnSpecification.GetNameColumn(), GridColumnSpecification.GetCustomNameColumn(), GridColumnSpecification.GetActionsColumn() },
                    GridViewModel.GRID_TITLES_EDISOVERY_PACKAGES)
                );

            // Now we build up the list of audit log entries.
            var auditLogEntries = this.auditLogStore.TranslateEntriesForDisplay(
                    await this.auditLogStore.GetEntries(folder.Identifier, ModuleType.eDiscovery), userTimeZone
                );

            this.page.Views.Add(AuditLogViewBuilder.BuildPagedGridView(pageIndex, pageSize, auditLogEntries, folder.Identifier));
        }
    }
}
