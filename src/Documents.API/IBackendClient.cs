namespace Documents.API
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Documents.API.Models;
    using Documents.API.Common.Models.MetadataModels;
    using System.Collections.Generic;

    public interface IBackendClient
    {
        Task DeleteFileAsync(
            BackendConfiguration context, 
            string id, 
            CancellationToken cancellationToken = default(CancellationToken)
        );

        Task ReadFileAsync(
            BackendConfiguration context, 
            string id, 
            Stream stream, 
            long from, 
            long to, 
            long totalLength, 
            CancellationToken cancellationToken = default(CancellationToken)
        );



        // 3 part upload
        Task<string> StartChunkedUploadAsync(
            BackendConfiguration context, 
            string id, 
            ChunkedUploadModel chunkHeader, 
            CancellationToken cancellationToken = default(CancellationToken)
        );
        Task<string> UploadChunkAsync(
            BackendConfiguration context, 
            string uploadKey, 
            string id, 
            string chunkKey, 
            int chunkIndex, 
            int totalChunks, 
            string sequentialState, 
            long rangeFrom, 
            long rangeTo, 
            long rangeTotal, 
            Stream stream, 
            CancellationToken cancellationToken = default(CancellationToken)
        );
        Task<IDictionary<string, object>> CompleteChunkedUploadAsync(
            BackendConfiguration context,
            string uploadKey,
            string id,
            ChunkedStatusModel[] chunkStatuses,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        Task<bool> SetTagsAsync(
            BackendConfiguration context,
            string id,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        Task<Dictionary<string, string>> GetTagsAsync(
            BackendConfiguration context,
            string id,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        Task<FileBackendConstants.OnlineStatus> GetOnlineStatusAsync(
            BackendConfiguration context,
            string id,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        Task<bool> RequestOnlineAsync(
            BackendConfiguration context,
            string id,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        Task<bool> CheckHealthAsync(
            BackendConfiguration context
        );
    }
}