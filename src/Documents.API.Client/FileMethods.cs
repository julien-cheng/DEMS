namespace Documents.API.Client
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class FileMethods : RESTBase<FileModel, FileIdentifier>
    {
        private const int COPY_BUFFER_SIZE = 81920; // 80 kilobytes, default in framework, but need to specify for cancellation
        public FileMethods(Connection connection)
            : base(connection, APIEndpoint.File)
        { }

        public Task<bool> MoveAsync(FileIdentifier destination, FileIdentifier source)
        => Connection.APICallAsync<bool>(HttpMethod.Post, APIEndpoint.FileMove, new { destination, source });

        public Task<UploadContextModel> UploadBeginAsync(
            FileModel fileModel, 
            CancellationToken cancellationToken = default(CancellationToken))
        => Connection.APICallAsync<UploadContextModel>(HttpMethod.Post, APIEndpoint.FileUploadBegin,
                bodyContent: fileModel, cancellationToken: cancellationToken);

        public async Task<UploadContextModel> UploadSendChunkAsync(
            UploadContextModel uploadContext,
            int chunkIndex,
            long rangeFrom, 
            long rangeTo,
            Stream stream,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var request = await Connection.RequestAsync(new HttpMethod("PATCH"), APIEndpoint.FileUploadChunk.Endpoint, new
            {
                uploadContext,
                chunkIndex,
            }))
            {
                // todo: hmm... will this let us reuse a stream at current offset? no good for retries.
                request.Content = new StreamContent(stream);

                request.Content.Headers.Add("Content-Range", 
                    new ContentRangeHeaderValue(
                        rangeFrom, 
                        rangeTo, 
                        uploadContext.FileLength
                    ).ToString()
                );
                request.Headers.TransferEncodingChunked = true;

                using (var response = await Connection.SendAsync(request))
                {
                    await Connection.DocumentsEnsureSuccessStatus(response);
                    return await Connection.ReadAsAsync<UploadContextModel>(response);
                }
            }
        }

        public Task<FileModel> UploadEndAsync(UploadContextModel uploadContext, CancellationToken cancellationToken = default(CancellationToken))
            => Connection.APICallAsync<FileModel>(HttpMethod.Post, APIEndpoint.FileUploadEnd, bodyContent: uploadContext, cancellationToken: cancellationToken);


        public async Task<T> DownloadAsAsync<T>(FileIdentifier fileIdentifer)
        {
            T result = default(T);

            var serializer = new JsonSerializer();
            await DownloadAsync(fileIdentifer, (stream, cancel) =>
            {

                using (var sr = new StreamReader(stream))
                using (var jsonTextReader = new JsonTextReader(sr))
                    result = serializer.Deserialize<T>(jsonTextReader); // uhoh, is there no async version here?

                return Task.FromResult(0);
            });

            return result;
        }

        public Task DownloadAsync(
            FileIdentifier identifier,
            Stream stream,
            CancellationToken cancellationToken = default(CancellationToken))
        => DownloadAsync(
            identifier,
            (downloadStream, cancel) => downloadStream.CopyToAsync(stream, COPY_BUFFER_SIZE, cancel),
            cancellationToken: cancellationToken
        );

        public async Task DownloadAsync(
            FileIdentifier identifier,
            Func<Stream, CancellationToken, Task> onStreamAvailable,
            long? from = 0,
            long? to = null,
            Action<DownloadHeaderInformation> onDownloadHeaderInformation = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using (var request = await Connection.RequestAsync(HttpMethod.Get, APIEndpoint.FileDownload.Endpoint, queryString: identifier ))
            {
                request.Headers.Add("Range", new RangeHeaderValue(from, to).ToString());

                using (var response = await Connection.SendAsync(request, cancellationToken))
                {
                    await Connection.DocumentsEnsureSuccessStatus(response);

                    var content = response.Content;
                    var filename = content.Headers.ContentDisposition.FileName;
                    if (filename != null && filename.StartsWith("\"") && filename.EndsWith("\""))
                        filename = filename.Substring(1, filename.Length - 2);

                    var contentRange = response.Content.Headers.ContentRange;
                    if (contentRange == null)
                        throw new Exception("API did not return a Content-Range header");

                    onDownloadHeaderInformation?.Invoke(new DownloadHeaderInformation
                    {
                        ContentLength = response.Content.Headers.ContentLength.Value,
                        RangeFrom = contentRange.From.Value,
                        RangeTo = contentRange.To.Value,
                        RangeLength = contentRange.Length.Value,
                        ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                        FileName = filename
                    });

                    var contents = await content.ReadAsStreamAsync();

                    await onStreamAvailable(contents, cancellationToken);
                }
            }
        }

        public Task<FileModel> FileModelFromLocalFileAsync(string fileName, FileIdentifier identifier)
        {
            // todo: is there really no async version of this?
            var info = new FileInfo(fileName);
            var file = new FileModel()
                {
                    Identifier = identifier,
                    Name = info.Name,
                    Created = info.CreationTimeUtc,
                    Modified = info.LastWriteTimeUtc,
                    Length = info.Length,
                    FilePrivileges = new Dictionary<string, IDictionary<string, IEnumerable<ACLModel>>>()
                }
                .InitializeEmptyMetadata()
                .InitializeEmptyPrivileges();

            return Task.FromResult(file);
        }

        public Task<int> GetUploadChunkSizeAsync(OrganizationIdentifier organizationIdentifier, CancellationToken cancellationToken = default(CancellationToken))
            => Connection.APICallAsync<int>(HttpMethod.Get, APIEndpoint.FileUploadChunkSize, queryStringContent: organizationIdentifier, cancellationToken: cancellationToken);

        public async Task<FileModel> UploadAsync(
            FileModel fileModel,
            string contents,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(contents)))
            {
                fileModel.Length = ms.Length;
                return await this.UploadAsync(fileModel, ms, cancellationToken);
            }
        }

        public async Task<FileModel> UploadAsync(
            FileModel fileModel,
            Stream seekableStream,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var uploadContext = await UploadBeginAsync(fileModel, cancellationToken);
            //Logger?.LogDebug($"Upload Token: {token}");

            var position = 0L;
            var chunkSize = uploadContext.ChunkSize;
            var buffer = new byte[chunkSize];
            int chunkIndex = 0;

            int totalChunks = (int)(uploadContext.FileLength / chunkSize)
                + ((uploadContext.FileLength % chunkSize == 0)
                    ? 0
                    : 1);

            while (position < uploadContext.FileLength)
            {
                int retries = 0;
                bool success = false;
                while (!success)
                {
                    try
                    {
                        seekableStream.Seek(position, SeekOrigin.Begin);
                        long from = position;
                        long to = Math.Min(position + chunkSize - 1, uploadContext.FileLength - 1);

                        int bytes = 0;

                        // can't this come straight from a seekable file? 
                        while ((bytes = await seekableStream.ReadAsync(buffer, bytes, buffer.Length - bytes)) > 0) ;

                        //Logger?.LogDebug($"FileChunkedUpload({token}, {from}, {to}, {fileInfo.Length}, stream)");
                        using (var ms = new MemoryStream(buffer))
                        {
                            long length = Math.Min(to - from + 1, chunkSize);
                            //Logger?.LogDebug($"stream length: {length}");
                            ms.SetLength(length);

                            uploadContext = await UploadSendChunkAsync(
                                uploadContext,
                                chunkIndex,
                                from,
                                to,
                                ms,
                                cancellationToken
                            );
                        }
                        success = true;
                        chunkIndex++;

                        position += chunkSize;
                    }
                    catch (Exception e)
                    {
                        //Logger?.LogWarning($"Failure during upload: {e}");
                        if (++retries == 10)
                            throw new Exception($"aborting after too many failures on upload", e);
                    }
                }
            }

            //Logger?.LogDebug($"FileChunkedEnd({uploadContext})");
            var file = await UploadEndAsync(uploadContext, cancellationToken);

            //Logger?.LogDebug($"file({JsonConvert.SerializeObject(file, Formatting.Indented)})");

            return file;
        }

        public async Task<FileModel> UploadLocalFileAsync(
            string localFilePath,
            FileModel fileModel,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var fileInfo = new FileInfo(localFilePath);

            var uploadContext = await UploadBeginAsync(fileModel, cancellationToken);
            //Logger?.LogDebug($"Upload Token: {token}");

            var position = 0L;
            var chunkSize = uploadContext.ChunkSize;
            var buffer = new byte[chunkSize];
            int chunkIndex = 0;

            int totalChunks = (int)(fileInfo.Length / chunkSize)
                + ((fileInfo.Length % chunkSize == 0)
                    ? 0
                    : 1);

            using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                while (position < fileInfo.Length)
                {
                    int retries = 0;
                    bool success = false;
                    while (!success)
                    {
                        try
                        {
                            fileStream.Seek(position, SeekOrigin.Begin);
                            long from = position;
                            long to = Math.Min(position + chunkSize - 1, fileInfo.Length - 1);

                            int bytes = 0;

                            // todo: why do we have an extra byte buffer and memory stream here? 
                            // can't this come straight from a seekable file? 

                            while ((bytes = await fileStream.ReadAsync(buffer, bytes, buffer.Length - bytes)) > 0) ;

                            //Logger?.LogDebug($"FileChunkedUpload({token}, {from}, {to}, {fileInfo.Length}, stream)");

                            using (var ms = new MemoryStream(buffer))
                            {
                                long length = Math.Min(to - from + 1, chunkSize);
                                //Logger?.LogDebug($"stream length: {length}");
                                ms.SetLength(length);

                                uploadContext = await UploadSendChunkAsync(
                                    uploadContext,
                                    chunkIndex,
                                    from,
                                    to,
                                    ms,
                                    cancellationToken
                                );
                            }
                            success = true;
                            chunkIndex++;

                            position += chunkSize;
                        }
                        catch (Exception e)
                        {
                            //Logger?.LogWarning($"Failure during upload: {e}");
                            if (++retries == 10)
                                throw new Exception($"aborting after too many failures on upload", e);
                        }
                    }
                }
            }

            //Logger?.LogDebug($"FileChunkedEnd({uploadContext})");
            var file = await UploadEndAsync(uploadContext, cancellationToken);

            //Logger?.LogDebug($"file({JsonConvert.SerializeObject(file, Formatting.Indented)})");

            return file;
        }

        public Task<bool> SetTagsAsync(
            FileIdentifier identifier,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken = default(CancellationToken)
        )
            => Connection.APICallAsync<bool>(
                HttpMethod.Post,
                APIEndpoint.FileTag,
                new { identifier },
                tags,
                cancellationToken: cancellationToken
            );

        public Task<Dictionary<string, string>> GetTagsAsync(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
            => Connection.APICallAsync<Dictionary<string, string>>(
                HttpMethod.Get,
                APIEndpoint.FileTag,
                new { identifier },
                cancellationToken: cancellationToken
            );

        public Task<FileModel.OnlineStatus> GetOnlineStatusAsync(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
            => Connection.APICallAsync<FileModel.OnlineStatus>(
                HttpMethod.Get,
                APIEndpoint.FileOnlineStatus,
                new { identifier },
                cancellationToken: cancellationToken
            );

        public Task<bool> RequestOnlineStatusAsync(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
            => Connection.APICallAsync<bool>(
                HttpMethod.Post,
                APIEndpoint.FileOnlineStatus,
                new { identifier },
                cancellationToken: cancellationToken
            );

    }
}
