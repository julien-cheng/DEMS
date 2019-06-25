namespace Documents.Clients.Manager.Common
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using System;
    using System.Collections.Generic;

    public static class APIModelExtensions
    {

        public static PathIdentifier MetaPathIdentifierRead(this IDictionary<string, string> metadata, FolderIdentifier folderIdentifier, bool dontThrow = false)
        {
            return new PathIdentifier(
                folderIdentifier.OrganizationKey,
                folderIdentifier.FolderKey,
                metadata.Read<string>("_path") ?? string.Empty,
                dontThrow: dontThrow
            );
        }

        public static PathIdentifier MetaPathIdentifierRead(this FileModel fileModel)
        {
            return fileModel.MetadataFlattened.MetaPathIdentifierRead(fileModel.Identifier as FolderIdentifier);
        }

        public static void MetaPathIdentifierWrite(this FileModel fileModel, PathIdentifier pathIdentifier)
        {
            if (pathIdentifier.OrganizationKey != fileModel.Identifier.OrganizationKey
                || pathIdentifier.FolderKey != fileModel.Identifier.FolderKey)
                throw new Exception("PathIdentifier does not reconcile with file's Identifier wrt: OrganizationKey or FolderKey");

            fileModel.Write("_path", pathIdentifier.PathKey);
        }

        public static FolderModel MetaExternalUserListWrite(this FolderModel folder, List<ExternalUser> externalUsers, string metadataKeyLocation)
        {
            folder.Write(metadataKeyLocation, externalUsers);
            return folder;
        }

        public static List<ExternalUser> MetaExternalUserListRead(this FolderModel folder, string metadataKeyLocation, bool includeHash = true)
        {
            var externalUsers = folder.Read<List<ExternalUser>>(metadataKeyLocation, defaultValue: new List<ExternalUser>());

            if (!includeHash)
                foreach (var externalUser in externalUsers)
                    externalUser.PasswordHash = null;

            return externalUsers;
        }

        public static void MetaExternalUserListRemove(this FolderModel folder, string email, string metadataKeyLocation)
        {
            var externalUsers = folder.MetaExternalUserListRead(metadataKeyLocation);
            externalUsers.RemoveAll(r => r.Email.ToLower() == email.ToLower());
            folder.MetaExternalUserListWrite(externalUsers, metadataKeyLocation);
        }

        public static void MetaExternalUserListUpsert(this FolderModel folder, ExternalUser externalUser, string metadataKeyLocation)
        {
            var externalUsers = folder.MetaExternalUserListRead(metadataKeyLocation);
            externalUsers.RemoveAll(r => r.Email?.ToLower() == externalUser.Email?.ToLower());
            externalUsers.Add(externalUser);
            folder.MetaExternalUserListWrite(externalUsers, metadataKeyLocation);
        }

        public static FileIdentifier MetaChildOfRead(this FileModel fileModel)
        {
            return fileModel.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF);
        }

        public static FileModel MetaChildOfWrite(this FileModel fileModel, FileIdentifier parentIdentifier)
        {
            fileModel.Write<FileIdentifier>(MetadataKeyConstants.CHILDOF, parentIdentifier);
            return fileModel;
        }

        public static bool MetaHiddenRead(this FileModel fileModel)
        {
            return fileModel.Read<bool>(MetadataKeyConstants.HIDDEN);
        }

        public static FileModel MetaHiddenWrite(this FileModel fileModel, bool isHidden)
        {
            fileModel.Write(MetadataKeyConstants.HIDDEN, isHidden ? (object)true : null);
            return fileModel;
        }
    }
}
