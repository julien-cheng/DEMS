namespace Documents.Backends.Drivers.FileSystem
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public class Driver : IFileBackend
    {
        private const int COPY_BUFFER_SIZE = 81920;

        public ILogger Logger { get; set; }

        Task<string> IFileBackend.ChunkedUploadStartAsync(
            object context,
            string id
        )
        {
            var config = context as Context;
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        object IFileBackend.CreateContext(string jsonConfiguration)
        {
            var context = new Context();
            context.Configure(jsonConfiguration);

            return context;
        }

        async Task<string> IFileBackend.ChunkedUploadChunkAsync(
            object context,
            string id,
            string uploadKey,
            string chunkKey,
            int chunkIndex,
            int totalChunks,
            string sequentialState,
            long from,
            long to,
            long totalLength,
            Stream stream,
            CancellationToken cancellationToken
        )
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var config = context as Context;
            using (var fileStream = OpenFile(GetFilePath(context as Context, id)))
            {
                fileStream.Seek(from, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream, COPY_BUFFER_SIZE, cancellationToken);
            }

            return string.Empty;
        }

        private FileStream OpenFile(string path)
        {
            return new FileStream(path, FileMode.OpenOrCreate);
        }

        private string GetFilePath(Context context, string id)
        {
            return Path.Combine(context.BasePath, id);
        }

        Task<IDictionary<string, object>> IFileBackend.ChunkedUploadCompleteAsync(
            object context,
            string uploadKey,
            string id,
            IChunkStatus[] chunkStatuses,
            CancellationToken cancellationToken
        )
        {
            var config = context as Context;

            Logger?.LogInformation($"Upload done.");

            return Task.FromResult(null as IDictionary<string, object>);
        }

        async Task IFileBackend.ReadFileAsync(
            object context, 
            string id, 
            Stream stream, 
            long from, 
            long to,
            long totalLength,
            CancellationToken cancellationToken)
        {
            var config = context as Context;

            using (var fileStream = OpenFile(GetFilePath(config, id)))
            {
                fileStream.Seek(from, SeekOrigin.Begin);
                await fileStream.CopyToAsync(stream, COPY_BUFFER_SIZE, cancellationToken);
            }
        }

        Task IFileBackend.DeleteFileAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;

            File.Delete(GetFilePath(config, id));

            return Task.FromResult(0);
        }

        Task<bool> IFileBackend.SetTagsAsync(object context, string id, Dictionary<string, string> tags, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<Dictionary<string, string>> IFileBackend.GetTagsAsync(object context, string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Dictionary<string, string>());
        }

        Task<FileBackendConstants.OnlineStatus> IFileBackend.GetOnlineStatusAsync(object context, string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(FileBackendConstants.OnlineStatus.Online);
        }

        Task<bool> IFileBackend.RequestOnlineAsync(object context, string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}