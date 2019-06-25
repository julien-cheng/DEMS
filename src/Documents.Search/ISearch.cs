namespace Documents.Search
{
    using Documents.API.Common.Models;
    using System.Threading.Tasks;

    public interface ISearch
    {
        Task IndexFileAsync(
            FolderModel folderModel,
            FileModel fileModel,
            string text
        );

        Task DeleteFileAsync(
            string[] securityIdentifiers,
            FileIdentifier fileIdentifier
        );

        Task DeleteEntireIndex();

        Task DeleteFolderAsync(
            string[] securityIdentifiers,
            FolderIdentifier folderIdentifier
        );

        Task DeleteOrganizationAsync(
            string[] securityIdentifiers,
            OrganizationIdentifier organizationIdentifier
        );

        Task<ISearchResults> Search(
            string[] securityIdentifiers, 
            SearchRequest searchRequest
        );
    }
}
