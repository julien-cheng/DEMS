namespace Documents.Clients.Manager.Modules.ItemQueryHandlers
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Services;
    using Documents.Clients.Manager.Services.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class BaseItemQueryHandler
    {
        protected APIConnection connection;
        private readonly ManagerConfiguration ManagerConfiguration;
        protected PathService pathService;
        protected FileService fileService;
        protected PathServiceState state;
        protected ItemQueryResponse page;
        protected IEnumerable<FileModel> filteredFiles;
        protected List<IItemQueryResponse> allRows = new List<IItemQueryResponse>();

        public static readonly string PATH_NAME_VALIDATION_PATTERN = "_pathNameValidation[regex]";
        public static readonly string FILE_NAME_VALIDATION_PATTERN = "_fileNameValidation[regex]";

        protected virtual bool BuildDataView { get; } = true;

        public BaseItemQueryHandler(PathService pathService, APIConnection connection, ManagerConfiguration managerConfiguration, FileService fileService)
        {
            this.connection = connection;
            this.ManagerConfiguration = managerConfiguration;
            this.pathService = pathService;
            this.fileService = fileService;
        }

        public virtual async Task<ItemQueryResponse> HandleItemQuery(
            PathIdentifier identifier,
            List<IModule> activeModules,
            bool isEdiscoveryUser = false,
            int pageIndex = 0,
            int pageSize = 5000,
            string sortField = "Name",
            bool sortAscending = true,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            await Initialize(identifier);

            FilterFiles(identifier);

            BuildPathValidationPatterns(activeModules);
            BuildFileValidationPatterns(activeModules);

            var attributeLocatorList = state.Folder.Read<List<AttributeLocator>>(MetadataKeyConstants.ATTRIBUTE_LOCATORS);

            var organization = await connection.Organization.GetAsync(identifier as OrganizationIdentifier);

            foreach (var file in filteredFiles)
                allRows.Add(fileService.ModelConvert(organization, file, connection.UserTimeZone, activeModules, identifier, attributeLocators: attributeLocatorList));

            AddSubpaths(activeModules, identifier);

            BuildDynamicFolders(activeModules, identifier, page, state.Folder);

            await BuildViews(activeModules, pageIndex, pageSize, state.Folder, identifier, connection.UserTimeZone);

            // Get the allowed operations for the path. 
            page.AllowedOperations = GetPageAllowedOperations(identifier);

            foreach (var module in activeModules)
                module.OnResponse(page, identifier);

            return page;
        }

        protected virtual void BuildDynamicFolders(List<IModule> activeModules, PathIdentifier identifier, ItemQueryResponse page, FolderModel folder)
        {
            foreach (var module in activeModules)
            {
                // False here signals that this is not the restricted view.  So for instance with eDiscovery the not shared yet folder will be included.
                module.BuildDynamicFolders(identifier, page, state.Folder, false);
            }
        }

        protected virtual void BuildPathValidationPatterns(List<IModule> modules)
        {
            var patternsMetaValue = state.Folder.Read<string>(PATH_NAME_VALIDATION_PATTERN);
            var patternOverrides = new List<PatternRegex>();

            if (patternsMetaValue == null)
                patternOverrides.Add(new PatternRegex() { Pattern = "^[a-zA-Z0-9-_. ]+", IsAllowed = true });
            else
                patternOverrides = JsonConvert.DeserializeObject<List<PatternRegex>>(patternsMetaValue);

            // Now we go through our modules, and append any of their path validation patterns.
            foreach (var module in modules)
            {
                patternOverrides.AddRange(module.GetPathValidationPatterns());
            }

            page.PathNameValidationPatterns = patternOverrides;
        }

        protected virtual void BuildFileValidationPatterns(List<IModule> modules)
        {
            var patternsMetaValue = state.Folder.Read<string>(FILE_NAME_VALIDATION_PATTERN);
            var patternOverrides = new List<PatternRegex>();
            if(patternsMetaValue != null)
            {
                patternOverrides = JsonConvert.DeserializeObject<List<PatternRegex>>(patternsMetaValue);
            }

            // Now we go through our modules, and append any of their file validation patterns.
            foreach (var module in modules)
            {
                patternOverrides.AddRange(module.GetFileValidationPatterns());
            }

            page.FileNameValidationPatterns = patternOverrides;
        }

        protected virtual async Task Initialize(PathIdentifier identifier)
        {
            state = await pathService.OpenFolder(identifier);
            page = new ItemQueryResponse
            {
                PathName = identifier.LeafName,
                PathTree = state.Paths.Root,
            };

            if (state.Folder != null)
            {
                var caseNumber = state.Folder.Read<string>("attribute.casenumber");
                var lastName = state.Folder.Read<string>("attribute.lastname");

                if (lastName != null)
                {
                    if (caseNumber != null)
                        page.PathTree.Name = $"{lastName} ({caseNumber})";
                    else
                        page.PathTree.Name = lastName;
                }

            }

            filteredFiles = state.Folder.Files.Rows as IEnumerable<FileModel>; //A collection of files that we will use to build up our response.
        }

        protected virtual List<AllowedOperation> GetPageAllowedOperations(PathIdentifier identifier)
        {
            var ops = new List<AllowedOperation>
            {
                AllowedOperation.GetAllowedOperationMove(identifier),
                AllowedOperation.GetAllowedOperationNewPath(identifier)
            };

            if (ManagerConfiguration.IsFeatureEnabledUpload
                && state.Folder.PrivilegeCheck("file:create", connection, throwOnFail: false))
                ops.Add(AllowedOperation.GetAllowedOperationUpload(identifier));
            
            if (ManagerConfiguration.IsFeatureEnabledSearch)
                ops.Add(AllowedOperation.GetAllowedOperationSearch(identifier));

            return ops;
        }

        protected virtual void FilterFiles(PathIdentifier identifier)
        {
            filteredFiles = filteredFiles
                .Where(f => identifier.Equals(f.MetaPathIdentifierRead()))
                .Where(f => !f.MetaHiddenRead());
        }

        protected virtual Task BuildViews(List<IModule> activeModules, int pageIndex, int pageSize, FolderModel folder, PathIdentifier identifier, string userTimeZone)
        {
            var views = new List<IViewModel>();

            if ((identifier.LeafName == null || identifier.LeafName == string.Empty) && BuildDataView)
            {
                var schema = DataViewBuilder.BuildDataViewModel(state.Folder);
                if (schema != null)
                    views.Add(schema);
            }

            var gridTitle = identifier.PathKey;
            if (String.IsNullOrEmpty(gridTitle))
            {
                gridTitle = "Case Files";
            }

            foreach (var module in activeModules)
            {
                module.AlterGridTitle(gridTitle, identifier);
            }


            // In this case we're going to have files/folders.  so we build up the columns accordingly.
            views.Add(GridViewBuilder.BuildGridView(pageIndex, pageSize, filteredFiles, allRows, GridColumnSpecification.GetStandarSetOfColumns(), gridTitle));

            page.Views = views;

            return Task.FromResult(0);
        }

        public virtual List<AllowedOperation> GetSubPathOperations(List<IModule> activeModules, PathIdentifier pathIdentifier)
        {
            var subPathDefaultOperations = new List<AllowedOperation>()
                {
                    AllowedOperation.GetAllowedOperationMove(pathIdentifier, pathIdentifier),
                    AllowedOperation.GetAllowedOperationRename(pathIdentifier),
                    AllowedOperation.GetAllowedOperationDownloadZip(pathIdentifier)
                };

            if (state.Folder.PrivilegeCheck("delete", connection, throwOnFail: false))
                subPathDefaultOperations.Add(AllowedOperation.GetAllowedOperationDelete(pathIdentifier));


            foreach (var module in activeModules)
            {
                module.AlterSubPathOperations(pathIdentifier, subPathDefaultOperations);
            }
            return subPathDefaultOperations;
        }

        protected virtual void AddSubpaths(List<IModule> activeModules, PathIdentifier identifier)
        {
            // add subpaths
            var requestedPath = state.Paths[identifier];
            if (requestedPath?.Paths != null)
            {
                foreach (var p in requestedPath.Paths)
                {
                    allRows.Add(new ManagerPathModel
                    {
                        Identifier = p.Identifier,
                        AllowedOperations = GetSubPathOperations(activeModules, p.Identifier),
                        FullPath = p.FullPath,
                        Icons = p.Icons,
                        Name = p.Name,
                        Paths = null
                    });
                }
            }

            foreach (var module in activeModules)
                module.FinalFilter(allRows, identifier);
        }
    }
}
