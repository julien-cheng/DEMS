namespace Documents.API.Common.Security
{
    using Documents.API.Common.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class SecurityExtensions
    {
        public static void WriteACLs(this FileModel model, string action, params string[] identifiers)
            => WriteACLs(model, action, new[] { new ACLModel(identifiers) });
        public static void WriteACLs(this FileModel model, string action, IEnumerable<ACLModel> acls)
            => model.FilePrivileges.PrivilegesForWriting(FileModel.Tier)
                .Write(action, acls);

        public static void WriteACLs(this FolderModel model, string action, params string[] identifiers)
            => WriteACLs(model, action, new[] { new ACLModel(identifiers) });
        public static void WriteACLs(this FolderModel model, string action, IEnumerable<ACLModel> acls)
            => model.FolderPrivileges.PrivilegesForWriting(FolderModel.Tier)
                .Write(action, acls);

        public static void WriteACLsForFile(this FolderModel model, string action, params string[] identifiers)
            => WriteACLsForFile(model, action, new[] { new ACLModel(identifiers) });
        public static void WriteACLsForFile(this FolderModel model, string action, IEnumerable<ACLModel> acls)
            => model.FilePrivileges.PrivilegesForWriting(FolderModel.Tier)
                .Write(action, acls);

        public static void WriteACLs(this OrganizationModel model, string action, params string[] identifiers)
            => WriteACLs(model, action, new[] { new ACLModel(identifiers) });
        public static void WriteACLs(this OrganizationModel model, string action, IEnumerable<ACLModel> acls)
            => model.OrganizationPrivileges.PrivilegesForWriting(OrganizationModel.Tier)
                .Write(action, acls);

        public static void WriteACLsForFolder(this OrganizationModel model, string action, params string[] identifiers)
            => WriteACLsForFolder(model, action, new[] { new ACLModel(identifiers) });
        public static void WriteACLsForFolder(this OrganizationModel model, string action, IEnumerable<ACLModel> acls)
            => model.FolderPrivileges.PrivilegesForWriting(OrganizationModel.Tier)
                .Write(action, acls);

        public static void WriteACLsForFile(this OrganizationModel model, string action, params string[] identifiers)
            => WriteACLsForFile(model, action, new[] { new ACLModel(identifiers) });
        public static void WriteACLsForFile(this OrganizationModel model, string action, IEnumerable<ACLModel> acls)
            => model.FilePrivileges.PrivilegesForWriting(OrganizationModel.Tier)
                .Write(action, acls);

        private static IDictionary<string, IEnumerable<ACLModel>> PrivilegesForWriting(
            this IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> tiers, 
            string tier
        )
        {
            if (tiers == null)
                throw new Exception("Model not loaded with Privileges. If you want to start from scratch, Initialize them to empty.");

            IDictionary<string, IEnumerable<ACLModel>> metadata;
            if (tiers.ContainsKey(tier))
                metadata = tiers[tier];
            else
            {
                metadata = new Dictionary<string, IEnumerable<ACLModel>>();
                tiers.Add(tier, metadata);
            }

            return metadata;
        }

        public static void Write(
            this IDictionary<string, IEnumerable<ACLModel>> privileges, 
            string action, 
            IEnumerable<ACLModel> acls
        )
        {
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException(nameof(action));

            if (privileges.ContainsKey(action.ToLower()))
                privileges[action.ToLower()] = acls;
            else
                privileges.Add(action, acls);
        }
    }
}
