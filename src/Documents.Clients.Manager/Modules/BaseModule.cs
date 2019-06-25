namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.AuditLog;
    using Documents.Clients.Manager.Modules.ItemQueryHandlers;
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public abstract class BaseModule : IModule
    {
        public virtual List<PatternRegex> GetFileValidationPatterns()
        {
            return new List<PatternRegex>();
        }

        public virtual List<PatternRegex> GetPathValidationPatterns()
        {
            return new List<PatternRegex>();
        }

        public abstract bool IsModuleActive(FolderModel folder);

        public abstract ModuleType ModuleType();

        public virtual void SetInitialAllowedOperations(FileModel fileModel, List<AllowedOperation> allowed, PathIdentifier virtualPathIdentifier)
        {
            // This is a NoOp in the base class we're not going to change anything about the allowed operations here.
        }

        public virtual void OverrideAllowedOperations(FileModel fileModel, List<AllowedOperation> allowed, PathIdentifier virtualPathIdentifier)
        {
           // This is a NoOp in the base class we're not going to change anything about the allowed operations here.
        }

        public virtual void BuildDynamicFolders(PathIdentifier identifier, ItemQueryResponse page, FolderModel folder, bool isRestrictedView = false)
        {
            // No op in the base case.
        }

        public virtual void AlterGridTitle(string gridTitle, PathIdentifier identifier)
        {
            // no op in base case.
        }

        public virtual void AlterSubPathOperations(PathIdentifier pathIdentifier, List<AllowedOperation> subPathDefaultOperations)
        {
            // no op in base case.
        }

        public virtual BaseItemQueryHandler GetInitialQueryHandler(PathIdentifier identifier, PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService)
        {
            return null;
        }

        public virtual BaseItemQueryHandler GetOverrideQueryHandler(PathIdentifier identifier, PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService)
        {
            return null;
        }

        public virtual Task<ModelBase> HandleBatchOperation(ModelBase operation)
        {
            return null;
            // no op.
        }

        public virtual bool HasHandlerForBatchOperation(ModelBase operation)
        {
            return false;
        }

        public virtual Task PreUploadAsync(FolderModel folderModel, FileModel fileModel)
        {
            return Task.FromResult(0);
        }

        public virtual Task PostUploadAsync(FolderModel folderModel, FileModel fileModel)
        {
            return Task.FromResult(0);
        }

        public virtual void FinalFilter(List<IItemQueryResponse> allRows, PathIdentifier identifier)
        {
        }

        public virtual void OnResponse(ItemQueryResponse response, PathIdentifier pathIdentifier)
        {
        }
    }
}
