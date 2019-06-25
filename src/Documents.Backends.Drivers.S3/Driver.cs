
// This class is not static, however it is a singleton.

namespace Documents.Backends.Drivers.S3
{
    using Amazon.S3;
    using Amazon.S3.Model;
    using Documents.Backends.Drivers;
    using Gateway.Streams;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class Driver : IFileBackend
    {
        public ILogger Logger { get; set; }

        async Task<string> IFileBackend.ChunkedUploadStartAsync(
            object context,
            string id
        )
        {
            var config = context as Context;

            IAmazonS3 s3Client = config.S3;

            var initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = config.BucketName,
                Key = id
            };

            InitiateMultipartUploadResponse initResponse =
                await s3Client.InitiateMultipartUploadAsync(initiateRequest);

            return initResponse.UploadId;
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
            var config = context as Context;
            IAmazonS3 s3Client = config.S3;

            using (stream)
            using (var buffer = new BufferedStream(stream))
            {
                UploadPartRequest uploadRequest = new UploadPartRequest
                {
                    BucketName = config.BucketName,
                    Key = id,
                    UploadId = uploadKey,
                    PartNumber = chunkIndex + 1,
                    PartSize = stream.Length,
                    InputStream = buffer
                };

                Logger?.LogInformation($"Sending Part: {uploadRequest.PartNumber}");

                try
                {
                    var partResponse = await s3Client.UploadPartAsync(uploadRequest, cancellationToken);

                    return partResponse.ETag;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        async Task<FileBackendConstants.OnlineStatus> IFileBackend.GetOnlineStatusAsync(object context, string id, CancellationToken cancellationToken)
        {
            return await GetOnlineStatusAsync(context, id, cancellationToken);
        }

        private async Task<FileBackendConstants.OnlineStatus> GetOnlineStatusAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;
            IAmazonS3 s3Client = config.S3;

            var metadata = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = config.BucketName,
                Key = id
            });

            if (metadata.RestoreInProgress)
                return FileBackendConstants.OnlineStatus.Restoring;

            if (metadata.RestoreExpiration > DateTime.UtcNow)
                return FileBackendConstants.OnlineStatus.Online;

            if (metadata.StorageClass == S3StorageClass.Glacier)
                return FileBackendConstants.OnlineStatus.Offline;
            if (metadata.StorageClass == "DEEP_ARCHIVE")
                return FileBackendConstants.OnlineStatus.Offline;

            return FileBackendConstants.OnlineStatus.Online;
        }

        async Task<bool> IFileBackend.RequestOnlineAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;
            IAmazonS3 s3Client = config.S3;

            var status = await GetOnlineStatusAsync(context, id, cancellationToken);

            if (status == FileBackendConstants.OnlineStatus.Offline)
                await s3Client.RestoreObjectAsync(new RestoreObjectRequest
                {
                    BucketName = config.BucketName,
                    Key = id,
                    Days = 10
                });

            return true;
        }

        async Task<Dictionary<string,string>> IFileBackend.GetTagsAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;
            IAmazonS3 s3Client = config.S3;

            var taggingResponse = await s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
            {
                BucketName = config.BucketName,
                Key = id
            });

            var tags = new Dictionary<string, string>();
            foreach (var pair in taggingResponse.Tagging)
                if (!tags.ContainsKey(pair.Key))
                    tags.Add(pair.Key, pair.Value);

            return tags;
        }

        async Task<bool> IFileBackend.SetTagsAsync(
            object context,
            string id,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken
        )
        {
            var config = context as Context;
            IAmazonS3 s3Client = config.S3;

            var taggingResponse = await s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
            {
                BucketName = config.BucketName,
                Key = id
            });

            var tagset = taggingResponse?.Tagging ?? new List<Tag>();

            foreach (var key in tags.Keys)
            {
                string value = tags[key];

                if (value == null)
                    tagset = tagset.Where(t => t.Key != key).ToList();
                else
                {
                    var found = false;

                    foreach (var tag in tagset)
                        if (tag.Key == key)
                        {
                            found = true;
                            tag.Value = value;
                        }

                    if (!found)
                        tagset.Add(new Tag
                        {
                            Key = key,
                            Value = value
                        });
                }
            }

            await s3Client.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = config.BucketName,
                Key = id,
                Tagging = new Tagging
                {
                    TagSet = tagset
                },
            });

            return true;
        }

        async Task<IDictionary<string, object>> IFileBackend.ChunkedUploadCompleteAsync(
            object context,
            string uploadKey,
            string id,
            IChunkStatus[] chunkStatuses,
            CancellationToken cancellationToken
        )
        {
            var started = DateTime.Now;

            var config = context as Context;
            IAmazonS3 s3Client = config.S3;

            Logger?.LogInformation($"Completing upload ...");

            Logger?.LogInformation($"[{DateTime.Now.Subtract(started).TotalMilliseconds}] Assembling parts");
            var partList = chunkStatuses
                .Where(c => c.Success)
                .GroupBy(c => c.ChunkIndex)
                .Select(g => new PartETag(g.Key + 1, g.Last().State))
                .ToList();

            var completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = config.BucketName,
                Key = id,
                UploadId = uploadKey,
                PartETags = partList
            };

            Logger?.LogInformation($"[{DateTime.Now.Subtract(started).TotalMilliseconds}] calling complete");

            var completeUploadResponse =
              await s3Client.CompleteMultipartUploadAsync(completeRequest);

            Logger?.LogInformation($"[{DateTime.Now.Subtract(started).TotalMilliseconds}] CompleteMultipartUpload finished");

            Logger?.LogInformation($"Upload done.");

            return null;
        }

        // this could be replaced with Stream.CopyToAsync() but this has proven useful for debugging
        private async Task CopyBytesAsync(Stream source, Stream destination, long length, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                byte[] buffer = new byte[32768];
                int read;
                while (!cancellationToken.IsCancellationRequested
                    && length > 0
                    && (read = await source.ReadAsync(buffer, 0,
                        (int)Math.Min(buffer.Length, length), cancellationToken)) > 0)
                {
                    Logger.LogInformation($"reading {read} bytes, {length} remaining");
                    destination.Write(buffer, 0, read);
                    length -= read;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(0, e, $"Failed to copy stream");
            }
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

            using (var response = await config.S3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = config.BucketName,
                Key = id,
                ByteRange = totalLength == -1
                    ? null
                    : new ByteRange(from, to)
            }, cancellationToken))
            {
                Console.WriteLine($"ReadFileAsync: id:{id} response.ContentLength: {response.ContentLength}");

                //using (var bufferedResponse = new BufferedStream(response.ResponseStream))
                using (var responseStream = new StreamRequestWrapper(
                    response.ResponseStream, response.ContentLength, Logger
                ))
                    //await CopyBytesAsync(responseStream, stream, response.ContentLength, cancellationToken);
                    await response.ResponseStream.CopyToAsync(stream, 81920, cancellationToken);
                await stream.FlushAsync();
            }
        }

        async Task IFileBackend.DeleteFileAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;

            var response = await config.S3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = config.BucketName,
                Key = id
            });
        }
    }
}
