namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Modules.ItemQueryHandlers;
    using Documents.Clients.Manager.Modules.LEOUpload;
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;
    using System.Linq;

    public class LogReaderModule : BaseModule, IModule
    {
        public override ModuleType ModuleType() => Modules.ModuleType.LogReader;

        protected readonly APIConnection connection;
        protected readonly APIConnection privilegedConnection;
        protected readonly ManagerConfiguration managerConfiguration;
        protected readonly LogReaderService logReaderService;

        private const string LOG_READER_PATH_KEY = ":logs";

        public LogReaderModule(
            APIConnection connection, 
            APIConnection priveligedConnection, 
            ManagerConfiguration managerConfiguration,
            LogReaderService logReaderService
        )
        {
            this.connection = connection;
            this.managerConfiguration = managerConfiguration;
            // This privieged connection allows us to do things like add a user.
            this.privilegedConnection = priveligedConnection;
            this.logReaderService = logReaderService;
        }

        public override bool IsModuleActive(FolderModel folder)
        {
            //First we need to determine based on metadata whether leo upload is active for this folder.
            return true;
        }


        public override void BuildDynamicFolders(PathIdentifier identifier, ItemQueryResponse page, FolderModel folder, bool isRestrictedView = false)
        {

            if (EDiscoveryUtility.IsUserEDiscovery(connection.UserAccessIdentifiers)
                || LEOUploadUtility.IsUserLeo(connection.UserAccessIdentifiers))
                return;

            var logPathIdentifier = new PathIdentifier(identifier)
            {
                PathKey = LOG_READER_PATH_KEY
            };

            if (page.PathTree.Paths == null)
                page.PathTree.Paths = new List<ManagerPathModel>();

            // Last the parent / root node of 'eDiscovery'
            var logPath = new ManagerPathModel
            {
                Identifier = logPathIdentifier,
                Icons = new string[] { "fa-history" },
                Name = "Logs",
                FullPath = LOG_READER_PATH_KEY,
                AllowedOperations = null,
                Paths = new List<ManagerPathModel>(),
                IsExpanded = false
            };

            page.PathTree.Paths.Add(logPath);
        }

        public override BaseItemQueryHandler GetInitialQueryHandler(PathIdentifier identifier, PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService)
        {
            if (identifier.PathKey == LOG_READER_PATH_KEY)
                return new LogReaderQueryHandler(pathService, connection, managerConfiguration, fileService, logReaderService);

            return null;
        }
    }
}
