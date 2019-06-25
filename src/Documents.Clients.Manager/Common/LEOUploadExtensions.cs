
namespace Documents.Clients.Manager.Common
{
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using System.Collections.Generic;

    public static class LEOUploadExtensions
    {
        public static List<ExternalUser> MetaLEOUploadOfficerListRead(this FolderModel folder, bool includeHash = true)
        {
            return APIModelExtensions.MetaExternalUserListRead(folder, MetadataKeyConstants.LEO_UPLOAD_OFFICERS, includeHash);
        }

        public static void MetaLEOUploadOfficerListRemove(this FolderModel folder, string email)
        {
            APIModelExtensions.MetaExternalUserListRemove(folder, email, MetadataKeyConstants.LEO_UPLOAD_OFFICERS);
        }

        public static void MetaLEOUploadOfficerListUpsert(this FolderModel folder, ExternalUser officer)
        {
            APIModelExtensions.MetaExternalUserListUpsert(folder, officer, MetadataKeyConstants.LEO_UPLOAD_OFFICERS);
        }

        public static FolderModel MetaLEOUploadOfficerListWrite(this FolderModel folder, List<ExternalUser> officers)
        {
            return APIModelExtensions.MetaExternalUserListWrite(folder, officers, MetadataKeyConstants.LEO_UPLOAD_OFFICERS);
        }
    }
}
