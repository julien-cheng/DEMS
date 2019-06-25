namespace Documents.Store
{
    using Documents.API.Common.Models;
    using System.Threading.Tasks;

    public interface IUploadStore : IModelStore<UploadModel, UploadIdentifier>
    {
        Task Cleanup(UploadIdentifier identifier);
    }
}