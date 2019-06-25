namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.ItemQueryHandlers;
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;

    public class ModuleConfigurator
    {
        protected readonly APIConnection connection;
        protected readonly EDiscovery eDiscovery;
        protected readonly EArraignment eArraignment;
        protected readonly LEOUploadModule leoUpload;
        protected readonly LogReaderModule logReader;

        public ModuleConfigurator(EDiscovery eDiscovery, EArraignment eArraignment, LEOUploadModule leoUpload, APIConnection connection, LogReaderModule logReader)
        {
            this.connection = connection;
            this.eArraignment = eArraignment;
            this.eDiscovery = eDiscovery;
            this.leoUpload = leoUpload;
            this.logReader = logReader;
        }

        public List<IModule> GetActiveModules(FolderModel folderModel)
        {
            var allModules = this.GetAllModules();

            var activeModules = new List<IModule>();

            foreach (var module in allModules)
            {
                if (module.IsModuleActive(folderModel))
                {
                    activeModules.Add(module);
                }
            }

            return activeModules;
        }

        /// <summary>
        /// Sometimes you want to know all the possible modules, for instance in batch operations.
        /// we're not checking whether they're active or not, if a module needs to handle a batch operation it will, because the module was the one that sent that allowed operation
        /// down to the client.
        /// </summary>
        /// <returns></returns>
        public List<IModule> GetAllModules()
        {
            return new List<IModule>() {
                this.eDiscovery,
                this.eArraignment,
                this.leoUpload,
                this.logReader
            };
        }

        public BaseItemQueryHandler GetActiveHandler(PathIdentifier identifier, List<IModule> activeModules, PathService pathService, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService)
        {
            var handler = new BaseItemQueryHandler(pathService, connection, managerConfiguration, fileService);

            foreach (var module in activeModules)
            {
                var initalHandler = module.GetInitialQueryHandler(identifier, pathService, connection, auditLogStore, managerConfiguration, fileService);
                if(initalHandler != null)
                {
                    handler = initalHandler;
                }
            }

            foreach (var module in activeModules)
            {
                var overrideHandler = module.GetOverrideQueryHandler(identifier, pathService, connection, auditLogStore, managerConfiguration, fileService);
                if (overrideHandler != null)
                {
                    handler = overrideHandler;
                }
            }

            return handler;
        }
    }
}
