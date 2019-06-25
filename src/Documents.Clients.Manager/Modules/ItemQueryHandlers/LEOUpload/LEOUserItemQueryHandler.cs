
namespace Documents.Clients.Manager.Modules.ItemQueryHandlers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Modules.LEOUpload;
    using Documents.Clients.Manager.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class LEOUserItemQueryHandler : BaseItemQueryHandler
    {
        protected override bool BuildDataView => true; 

        private readonly IAuditLogStore auditLogStore;
        private readonly LEOUploadModule LEOUploadModule;

        private PathIdentifier OfficerPathIdentifier = null;
        private bool IsLEOUser = false;

        private ManagerConfiguration ManagerConfiguration;

        public LEOUserItemQueryHandler(
            PathService pathService, 
            APIConnection connection, 
            IAuditLogStore auditLogStore, LEOUploadModule leoUploadModule, ManagerConfiguration managerConfiguration, FileService fileService) : base(pathService, connection, managerConfiguration, fileService)
        {
            this.auditLogStore = auditLogStore;
            this.LEOUploadModule = leoUploadModule;
            this.ManagerConfiguration = managerConfiguration;
        }

        protected override List<AllowedOperation> GetPageAllowedOperations(PathIdentifier identifier)
        {
            if (IsLEOUser && identifier != null && identifier.IsChildOf(OfficerPathIdentifier))
            {
                var ops = new List<AllowedOperation>
                {
                    AllowedOperation.GetAllowedOperationMove(identifier),
                    AllowedOperation.GetAllowedOperationNewPath(identifier)
                };

                if (ManagerConfiguration.IsFeatureEnabledUpload
                    && state.Folder.PrivilegeCheck("file:create", connection, throwOnFail: false))
                    ops.Add(AllowedOperation.GetAllowedOperationUpload(identifier));

                return ops;
            }

            return null;
        }

        private bool IsAllowed(PathIdentifier pathIdentifier, bool includeAncestors = true)
        {
            return (OfficerPathIdentifier.IsChildOf(pathIdentifier) && includeAncestors)
                    || pathIdentifier.Equals(OfficerPathIdentifier)
                    || pathIdentifier.IsChildOf(OfficerPathIdentifier);
        }

        protected void SecurityPrunePaths(ManagerPathModel node)
        {
            node.Paths = node.Paths.Where(p => IsAllowed(p.Identifier))
                .ToList();

            if (!IsAllowed(node.Identifier, includeAncestors: false))
                node.AllowedOperations = null;

            foreach (var child in node.Paths)
                SecurityPrunePaths(child);
        }

        protected override void BuildDynamicFolders(List<IModule> activeModules, PathIdentifier identifier, ItemQueryResponse page, FolderModel folder)
        {
            base.BuildDynamicFolders(activeModules, identifier, page, folder);

            if (IsLEOUser)
                SecurityPrunePaths(page.PathTree);
        }
        protected override async Task Initialize(PathIdentifier identifier)
        {
            if (LEOUploadUtility.IsUserLeo(connection.UserAccessIdentifiers))
            {
                OfficerPathIdentifier = await LEOUploadModule.GetOfficerPathAsync(identifier);
                IsLEOUser = true;
            }

            state = await pathService.OpenFolder(identifier, false);
            
            page = new ItemQueryResponse
            {
                PathName = identifier.LeafName,
                PathTree = state.Paths.Root,
            };

            filteredFiles = state.Folder.Files.Rows as IEnumerable<FileModel>; //A collection of files that we will use to build up our response.
        }

        protected override void FilterFiles(PathIdentifier identifier)
        {
            if (IsLEOUser)
            {
                //if (identifier.IsChildOf(OfficerPathIdentifier))
                {
                    filteredFiles = filteredFiles
                        .Where(f => identifier.Equals(f.MetaPathIdentifierRead()))
                        .Where(f => !f.MetaHiddenRead())
                        .Where(f => f.MetaPathIdentifierRead()?.IsChildOf(OfficerPathIdentifier) ?? false);
                }
                //else
                  //  filteredFiles = new FileModel[0];
            }
        }

        protected override void AddSubpaths(List<IModule> activeModules, PathIdentifier identifier)
        {
            if (IsAllowed(identifier, includeAncestors: false))
                base.AddSubpaths(activeModules, identifier);
        }

        public override List<AllowedOperation> GetSubPathOperations(List<IModule> activeModules, PathIdentifier pathIdentifier)
        {
            if (IsAllowed(pathIdentifier, includeAncestors: false))
                return base.GetSubPathOperations(activeModules, pathIdentifier);
            else
                return null;
        }
    }
}

