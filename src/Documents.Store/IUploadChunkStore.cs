namespace Documents.Store
{
    using Documents.API.Common.Models;

    public interface IUploadChunkStore : IModelStore<UploadChunkModel, UploadChunkIdentifier> { }
}