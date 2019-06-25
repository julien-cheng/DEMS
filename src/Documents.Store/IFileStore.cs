namespace Documents.Store
{
    using Documents.API.Common.Models;
    using System.Threading.Tasks;

    public interface IFileStore : IModelStore<FileModel, FileIdentifier>
    {
        Task<string> FileLocatorGetAsync(FileIdentifier identifier);
        Task FileLocatorSetAsync(FileIdentifier identifier, string fileLocator);
        Task UploadingStatusSetAsync(FileIdentifier identifier, bool isUploading);
        Task HashSetAsync(FileIdentifier identifier, string md5, string sha1, string sha256);
        Task Move(FileIdentifier destination, FileIdentifier source);
    }
}