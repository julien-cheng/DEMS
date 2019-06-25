
namespace Documents.Clients.Manager.Modules.ItemQueryHandlers
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Common.PathStructure;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;
    using System.Linq;

    class EDiscoveryDatedPackageItemQueryHandler : BaseItemQueryHandler
    {
        private readonly ManagerConfiguration ManagerConfiguration;
        protected PathIdentifier PackageIdentifier = null;
        protected PathIdentifier RelativePathIdentifier = null;
        protected string PackageName = null;

        public EDiscoveryDatedPackageItemQueryHandler(PathService pathService, APIConnection connection,
            ManagerConfiguration managerConfiguration, FileService fileService) : base(pathService, connection, managerConfiguration, fileService)
        {
            this.ManagerConfiguration = managerConfiguration;
        }

        protected override List<AllowedOperation> GetPageAllowedOperations(PathIdentifier identifier)
        {
            var ops = new List<AllowedOperation>();

            if (ManagerConfiguration.IsFeatureEnabledSearch)
                AllowedOperation.GetAllowedOperationSearch(identifier);

            return ops;
        }

        protected override void AddSubpaths(List<IModule> activeModules, PathIdentifier identifier)
        {
            if (PackageIdentifier == null)
                return;

            var processor = new PathProcessor(identifier);

            processor.Read(state.Folder, skipFolderPaths: true, pathReader: f => {
                var sharePath = f.MetaEDiscoveryPathIdentifierRead();

                if (PackageIdentifier.PathKey != EDiscoveryUtility.E_DISCOVERY_ALL_PACKAGE_PATH_KEY
                    && f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE) != PackageName
                    && f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE) != null)
                    return null;

                if (sharePath != null)
                    return PackageIdentifier.CreateChild(sharePath.PathKey);
                else
                    return sharePath;
            });

            var paths = RelativePathIdentifier != null
                ? processor[PackageIdentifier.CreateChild(RelativePathIdentifier.PathKey)].Paths
                : processor[PackageIdentifier].Paths;

            foreach (var p in paths)
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

            base.AddSubpaths(activeModules, identifier);
        }

        protected override void FilterFiles(PathIdentifier identifier)
        {
            PackageIdentifier = EDiscoveryUtility.GetPackageIdentifier(identifier);
            RelativePathIdentifier = identifier?.RelativeTo(PackageIdentifier);
            PackageName = PackageIdentifier?.PathKey?.Replace("eDiscovery/", "");

            InternalFilterFiles(identifier);

            filteredFiles = filteredFiles
                .Where(f => EDiscoveryUtility.IsPackageRelativePath(f, RelativePathIdentifier))
                .Where(f => !f.Read<bool>(MetadataKeyConstants.HIDDEN));
        }

        protected virtual void InternalFilterFiles(PathIdentifier identifier)
        {
            filteredFiles = filteredFiles
                .Where(f => f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY) == EDiscoveryShareState.Published.ToString())
                .Where(f => PackageName == "all"
                    || f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_PACKGAGE) == PackageName
                );
        }

        public override List<AllowedOperation> GetSubPathOperations(List<IModule> activeModules, PathIdentifier pathIdentifier)
        {
            var subPathDefaultOperations = new List<AllowedOperation>()
            {
                AllowedOperation.GetAllowedOperationDownloadZip(pathIdentifier)
            };

            return subPathDefaultOperations;
        }
    }
}
