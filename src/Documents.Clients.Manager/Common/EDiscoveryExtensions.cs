namespace Documents.Clients.Manager.Common
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class EDiscoveryExtensions
    {
        public static List<ExternalUser> MetaEDiscoveryRecipientListRead(this FolderModel folder, bool includeHash = true)
        {
            return APIModelExtensions.MetaExternalUserListRead(folder, MetadataKeyConstants.E_DISCOVERY_RECIPIENT_LIST, includeHash);
        }

        public static void MetaEDiscoveryRecipientListRemove(this FolderModel folder, string email)
        {
            APIModelExtensions.MetaExternalUserListRemove(folder, email, MetadataKeyConstants.E_DISCOVERY_RECIPIENT_LIST);
        }

        public static void MetaEDiscoveryRecipientListUpsert(this FolderModel folder, ExternalUser recipient)
        {
            APIModelExtensions.MetaExternalUserListUpsert(folder, recipient, MetadataKeyConstants.E_DISCOVERY_RECIPIENT_LIST);
        }

        public static FolderModel MetaEDiscoveryRecipientListWrite(this FolderModel folder, List<ExternalUser> recipientList)
        {
            return APIModelExtensions.MetaExternalUserListWrite(folder, recipientList, MetadataKeyConstants.E_DISCOVERY_RECIPIENT_LIST);
        }

        public static EDiscoveryShareState ShareState(this FileModel fileModel)
        {
            var state = fileModel.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY);
            if (Enum.TryParse(state, out EDiscoveryShareState result))
                return result;
            else
                return EDiscoveryShareState.NotShared;

        }
    }
}
