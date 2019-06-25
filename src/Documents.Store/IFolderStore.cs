namespace Documents.Store
{
    using Documents.API.Common.Models;

    public interface IFolderStore : IModelStore<FolderModel, FolderIdentifier>
    {
        string PrivilegeRead { get; set; }
    }
}