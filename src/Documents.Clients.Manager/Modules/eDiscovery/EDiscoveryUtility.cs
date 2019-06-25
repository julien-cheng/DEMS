namespace Documents.Clients.Manager.Modules.eDiscovery
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EDiscoveryUtility
    {
        public static readonly string E_DISCOVERY_PATH_NAME = "eDiscovery";
        public static readonly string E_DISCOVERY_PATH_KEY = "eDiscovery";
        public static readonly string E_DISCOVERY_NOT_SHARED_PATH_KEY = "eDiscovery/Not Shared Yet";
        public static readonly string E_DISCOVERY_DATED_PACKAGE_PATH_KEY = "eDiscovery/Discovery Package:";
        public static readonly string E_DISCOVERY_ALL_PACKAGE_PATH_KEY = "eDiscovery/all";

        public static readonly string E_DISCOVERY_DEFAULT_PASSWORD_LENGTH = "8";

        public static readonly string EDISCOVERY_FOLDER_COLOR_STYLE = "folder green";
        public static readonly string EDISCOVERY_ICON_COLOR_STYLE_STAGED = "green staged";
        public static readonly string EDISCOVERY_ICON_COLOR_STYLE_PUBLISHED = "green published";

        public static bool IsUserEDiscovery(string[] userAccessIdentifiers)
        {
            return userAccessIdentifiers?.Any(i => i == "r:eDiscovery") ?? false;
        }

        public static int GetStagedCount(FolderModel folder)
        {
            // We need to get the staged count, as it will allow us to A. determine if we're going to show the not shared yet folder,
            // and B. a cound on the not shared yet folder.
            return folder.Files.Rows
                .Where(f => f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY) == EDiscoveryShareState.Staged.ToString())
                .Count();
        }

        public static string GetCustomName(EDiscoveryPackageMap packageMap, string sharePackageName)
        {
            if (packageMap != null && packageMap.Map != null && packageMap.Map.ContainsKey(sharePackageName) && packageMap.Map[sharePackageName] != null)
            {
                return packageMap.Map[sharePackageName].CustomName;
            }
            return String.Empty;
        }

        public static EDiscoveryShareState GetCurrentShareState(FileModel file)
        {
            var shareState = file.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY);
            if (shareState == null)
            {
                return EDiscoveryShareState.NotShared;
            }
            return shareState != null ? (EDiscoveryShareState)Enum.Parse(typeof(EDiscoveryShareState), shareState) : EDiscoveryShareState.NotShared;
        }

        public static string GetShareStateIcon(FileModel fileModel)
        {
            var currentShareState = GetCurrentShareState(fileModel);
            switch (currentShareState)
            {
                case EDiscoveryShareState.NotShared:
                    return null;
                case EDiscoveryShareState.Staged:
                    return EDiscoveryUtility.EDISCOVERY_ICON_COLOR_STYLE_STAGED;
                case EDiscoveryShareState.Published:
                    return EDiscoveryUtility.EDISCOVERY_ICON_COLOR_STYLE_PUBLISHED;
                default:
                    return null;
            }
        }

        public static bool IsPackageRelativePath(FileModel fileModel, PathIdentifier relativePath)
        {
            var eDiscPath = fileModel.MetaEDiscoveryPathIdentifierRead();
            if (eDiscPath != null && eDiscPath.Equals(relativePath))
                return true;

            if (eDiscPath == null && relativePath == null)
                return true;

            return false;
        }

        public static PathIdentifier GetPackageIdentifier(PathIdentifier identifier)
        {
            var parents = identifier.ParentPathIdentifiers?.Reverse().ToList() ?? new List<PathIdentifier>();
            parents.Add(identifier);

            if (identifier.PathKey.StartsWith(EDiscoveryUtility.E_DISCOVERY_ALL_PACKAGE_PATH_KEY))
                return parents[2];
            else if (identifier.PathKey.StartsWith(EDiscoveryUtility.E_DISCOVERY_NOT_SHARED_PATH_KEY))
                return parents[2];
            else if (parents.Count > 4 && parents[1].PathKey == EDiscoveryUtility.E_DISCOVERY_PATH_KEY)
                return parents[4];
            else
                return null;
        }

        public static bool IsEDiscoveryPath(PathIdentifier pathIdentifier)
        {
            var path = pathIdentifier?.PathKey;

            return path != null && (
                path == EDiscoveryUtility.E_DISCOVERY_NOT_SHARED_PATH_KEY ||
                path == EDiscoveryUtility.E_DISCOVERY_ALL_PACKAGE_PATH_KEY ||
                path == EDiscoveryUtility.E_DISCOVERY_PATH_KEY ||
                path.StartsWith(EDiscoveryUtility.E_DISCOVERY_DATED_PACKAGE_PATH_KEY)
            );
        }

        public static PathIdentifier MetaEDiscoveryPathIdentifierRead(this FileModel fileModel)
        {
            var path = fileModel.MetadataFlattened.Read<string>(E_DISCOVERY_PATH_KEY);

            if (path == null)
                return null;

            return new PathIdentifier(
                fileModel.Identifier.OrganizationKey,
                fileModel.Identifier.FolderKey,
                path
            );
        }

        public static void MetaEDiscoveryPathIdentifierWrite(this FileModel fileModel, PathIdentifier pathIdentifier)
        {
            if (pathIdentifier != null
                && (
                    pathIdentifier.OrganizationKey != fileModel.Identifier.OrganizationKey
                    || pathIdentifier.FolderKey != fileModel.Identifier.FolderKey
                ))
                throw new Exception("PathIdentifier does not reconcile with file's Identifier wrt: OrganizationKey or FolderKey");

            fileModel.Write(E_DISCOVERY_PATH_KEY, pathIdentifier?.PathKey);
        }
    }
}
