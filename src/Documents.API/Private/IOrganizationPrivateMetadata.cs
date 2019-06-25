namespace Documents.API.Private
{
    using System.Threading.Tasks;
    using Documents.API.Common.Models;

    public interface IOrganizationPrivateMetadata
    {
        Task<FolderModel> PrivateFolderLoadAsync(OrganizationIdentifier organizationIdentifier);
        Task<FolderModel> PrivateFolderSaveAsync(FolderModel privateFolder);
    }
}