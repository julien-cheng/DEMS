
namespace Documents.Clients.Manager.Modules.ItemQueryHandlers
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Common.PathStructure;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class EDiscoveryUserItemQueryHandler : EDiscoveryDatedPackageItemQueryHandler
    {
        protected override bool BuildDataView => true;

        private readonly IAuditLogStore auditLogStore;
        private readonly EDiscovery eDiscovery;

        public EDiscoveryUserItemQueryHandler(PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, EDiscovery eDiscovery, ManagerConfiguration managerConfiguration, FileService fileService) : base(pathService, connection, managerConfiguration, fileService)
        {
            this.auditLogStore = auditLogStore;
            this.eDiscovery = eDiscovery;
        }

        protected override List<AllowedOperation> GetPageAllowedOperations(PathIdentifier identifier)
        {
            return null;
        }

        protected override void BuildDynamicFolders(List<IModule> activeModules, PathIdentifier identifier, ItemQueryResponse page, FolderModel folder)
        {
            foreach (var module in activeModules)
            {
                // true here means that this is a restricted view, so for things like EDiscovery the not shared yet folder will not be included.
                module.BuildDynamicFolders(identifier, page, state.Folder, true);
            }
        }

        protected override async Task Initialize(PathIdentifier identifier)
        {
            state = await pathService.OpenFolder(identifier, true);

            page = new ItemQueryResponse
            {
                PathName = identifier.LeafName,
                PathTree = state.Paths.Root,
            };
            // Only add an audit log entry if the user accessed a package. If full path == null, then they're hitting "Case Files"
            if (!String.IsNullOrEmpty(identifier.FullName))
            {
                await this.auditLogStore.AddEntry(
                    new AuditLogEntry()
                    {
                        EntryType = AuditLogEntryType.eDiscoveryUserPackageAccess,
                        Message = $"User Accessed Package: \"{identifier.PathKey.Replace(EDiscoveryUtility.E_DISCOVERY_PATH_NAME + "/", "")}\" ",
                        ModuleType = ModuleType.eDiscovery
                    },
                    identifier as FolderIdentifier
                );
            }

            filteredFiles = state.Folder.Files.Rows as IEnumerable<FileModel>; //A collection of files that we will use to build up our response.
        }

        protected override Task BuildViews(List<IModule> activeModules, int pageIndex, int pageSize, FolderModel folder, PathIdentifier identifier, string userTimeZone)
        {
            // We need to take a list of manager path models, and convert them to a list of item query response object
            var managerPathModels = this.eDiscovery.BuildDatedPackagesDynamicFolder(folder);

            // Now we need to convert these manager path models into something that's a list of 
            var dynamicPaths = new List<IItemQueryResponse>();

            dynamicPaths.AddRange(managerPathModels);

            this.page.Views = new List<IViewModel>();
            if (identifier.PathKey == EDiscoveryUtility.E_DISCOVERY_PATH_KEY)
            {
                this.page.Views.Add(GridViewBuilder.BuildGridView(pageIndex, pageSize, dynamicPaths, new List<AllowedOperation>(), new List<GridColumnSpecification>() {
                    GridColumnSpecification.GetNameColumn(), GridColumnSpecification.GetCustomNameColumn()
                    }, GridViewModel.GRID_TITLES_EDISOVERY_PACKAGES));
            }

            if (identifier.PathKey != null
                && (identifier.PathKey.StartsWith(EDiscoveryUtility.E_DISCOVERY_DATED_PACKAGE_PATH_KEY)
                    || identifier.PathKey.StartsWith(EDiscoveryUtility.E_DISCOVERY_ALL_PACKAGE_PATH_KEY)))
            {
                this.page.Views.Add(GridViewBuilder.BuildGridView(pageIndex, pageSize, filteredFiles, allRows, GridColumnSpecification.GetStandarSetOfColumns(), GridViewModel.GRID_TITLE_FILES));
            }

            return Task.FromResult(0);
        }
    }
}