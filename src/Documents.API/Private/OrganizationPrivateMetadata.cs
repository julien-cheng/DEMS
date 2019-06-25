namespace Documents.API.Private
{
    using Documents.API.Common.Models;
    using Documents.Store;
    using System.Threading.Tasks;

    public class OrganizationPrivateMetadata : IOrganizationPrivateMetadata
    {
        private readonly IFolderStore FolderStore;
        private const string PRIVATE_FOLDERKEY = ":private";

        public OrganizationPrivateMetadata(
            IFolderStore folderStore
        )
        {
            this.FolderStore = folderStore;
        }

        public async Task<FolderModel> PrivateFolderLoadAsync(OrganizationIdentifier organizationIdentifier)
        {
            var folderIdentifier = new FolderIdentifier(organizationIdentifier, PRIVATE_FOLDERKEY);
            FolderStore.PrivilegeRead = "gateway";

            var folder = await FolderStore.GetOneAsync(folderIdentifier);
            if (folder == null)
                folder = await FolderStore.InsertAsync(new FolderModel
                {
                    Identifier = folderIdentifier
                });

            return folder;
        }

        public Task<FolderModel> PrivateFolderSaveAsync(FolderModel privateFolder)
        {
            return FolderStore.UpdateAsync(privateFolder);
        }
    }
}
