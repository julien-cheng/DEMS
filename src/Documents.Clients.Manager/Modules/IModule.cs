

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

    public interface IModule
    {
        bool IsModuleActive(FolderModel folder);

        ModuleType ModuleType();

        List<PatternRegex> GetPathValidationPatterns();

        List<PatternRegex> GetFileValidationPatterns();

        void SetInitialAllowedOperations(FileModel fileModel, List<AllowedOperation> allowed, PathIdentifier virtualPathIdentifier);

        void OverrideAllowedOperations(FileModel fileModel, List<AllowedOperation> allowed, PathIdentifier virtualPathIdentifier);

        void BuildDynamicFolders(PathIdentifier identifier, ItemQueryResponse page, FolderModel folder, bool isRestrictedView);

        void AlterGridTitle(string gridTitle, PathIdentifier identifier);

        void AlterSubPathOperations(PathIdentifier pathIdentifier, List<AllowedOperation> subPathDefaultOperations);

        BaseItemQueryHandler GetInitialQueryHandler(PathIdentifier identifier, PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService);

        BaseItemQueryHandler GetOverrideQueryHandler(PathIdentifier identifier, PathService pathService, APIConnection connection, IAuditLogStore auditLogStore, ManagerConfiguration managerConfiguration, FileService fileService);

        Task<ModelBase> HandleBatchOperation(ModelBase operation);

        bool HasHandlerForBatchOperation(ModelBase operation);

        Task PreUploadAsync(FolderModel folderModel, FileModel fileModel);
        Task PostUploadAsync(FolderModel folderModel, FileModel fileModel);

        void FinalFilter(List<IItemQueryResponse> allRows, PathIdentifier identifier);
        void OnResponse(ItemQueryResponse response, PathIdentifier pathIdentifier);
    }
}
