namespace Documents.Backends.Drivers.Encryption
{
    using Documents.Backends.Drivers;
    using Gateway.Streams;
    using Microsoft.Extensions.Logging;
    using Org.BouncyCastle.Crypto.Digests;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class Driver : IFileBackend
    {
        private ILogger Logger = null;

        object IFileBackend.CreateContext(string jsonConfiguration)
        {
            var context = new Context();
            context.Configure(jsonConfiguration);

            var nextType = Type.GetType(context.NextDriverTypeName);
            context.NextDriver = Activator.CreateInstance(nextType) as IFileBackend;

            context.NextDriverContext = context.NextDriver.CreateContext(
                context.NextDriverConfigurationJson);

            context.NextDriver.Logger = Logger;

            return context;
        }

        ILogger IFileBackend.Logger
        {
            set
            {
                this.Logger = value;
            }
        }

        private List<GeneralDigest> PrepareDigests(Context config, SequentialState state, bool isFirstChunk)
        {
            var digests = new List<GeneralDigest>();

            if (config.MD5Enabled)
                digests.Add(state.MD5 = isFirstChunk ? new MD5Digest() : state.MD5);

            if (config.SHA1Enabled)
                digests.Add(state.SHA1 = isFirstChunk ? new Sha1Digest() : state.SHA1);

            if (config.SHA256Enabled)
                digests.Add(state.SHA256 = isFirstChunk ? new Sha256Digest() : state.SHA256);

            return digests;
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

            var state = new SequentialState(sequentialState);
            var digests = PrepareDigests(config, state, isFirstChunk: chunkIndex == 0);

            using (var encryptor = new ChunkEncryptorStreamReader(
                stream,
                config.CryptographyAlgorithm,
                from,
                to,
                totalLength,
                digests,
                state.IV(),
                Logger
            ))
            using (var wrapper = new StreamRequestWrapper(encryptor, encryptor.Length, Logger))
            {
                state.NextDriverState = await config.NextDriver.ChunkedUploadChunkAsync(
                    config.NextDriverContext, id, uploadKey, chunkKey, chunkIndex, totalChunks,
                    state.NextDriverState,
                    from, to, totalLength, wrapper,
                    cancellationToken
                );

                state.IVBase64 = Convert.ToBase64String(encryptor.NextIV());

                return state.ToString();
            }
        }

        async Task<IDictionary<string, object>> IFileBackend.ChunkedUploadCompleteAsync(
            object context,
            string uploadKey,
            string id,
            IChunkStatus[] chunkStatuses,
            CancellationToken cancellationToken
        )
        {
            var config = context as Context;
            var state = new SequentialState(chunkStatuses.Last().State);

            // Decode and remove this drivers layer of SequentialState
            for (int i = 0; i < chunkStatuses.Length; i++)
                chunkStatuses[i].State = new SequentialState(chunkStatuses[i].State).NextDriverState;

            var values = await config.NextDriver.ChunkedUploadCompleteAsync(
                config.NextDriverContext, uploadKey, id, chunkStatuses, cancellationToken
            );

            values = values ?? new Dictionary<string, object>();
            ReportDigestValues(values, state);

            return values;
        }

        private void ReportDigestValues(IDictionary<string, object> values, SequentialState state)
        {
            if (state.MD5 != null)
                values.Add("md5", GetDigestValue(state.MD5));
            if (state.SHA1 != null)
                values.Add("sha1", GetDigestValue(state.SHA1));
            if (state.SHA256 != null)
                values.Add("sha256", GetDigestValue(state.SHA256));
        }

        private string GetDigestValue(GeneralDigest digest)
        {
            var buffer = new byte[digest.GetDigestSize()];
            digest.DoFinal(buffer, 0);

            return Convert.ToBase64String(buffer);
        }

        async Task<string> IFileBackend.ChunkedUploadStartAsync(
            object context,
            string id
        )
        {
            var config = context as Context;

            return await config.NextDriver.ChunkedUploadStartAsync(config.NextDriverContext, id);
        }

        async Task IFileBackend.DeleteFileAsync(
            object context,
            string id,
            CancellationToken cancellationToken
        )
        {
            var config = context as Context;

            await config.NextDriver.DeleteFileAsync(config.NextDriverContext, id, cancellationToken);
        }

        async Task IFileBackend.ReadFileAsync(
            object context,
            string id,
            Stream stream,
            long from,
            long to,
            long totalLength,
            CancellationToken cancellationToken
        )
        {
            var config = context as Context;

            using (var decryptor = new DecryptingStreamWriter(
                stream,
                config.CryptographyAlgorithm,
                from,
                to,
                totalLength,
                Logger
            ))
            {
                await config.NextDriver.ReadFileAsync(
                    config.NextDriverContext, id, decryptor, 
                    decryptor.CipherFrom, decryptor.CipherTo,
                    decryptor.CipherTotalLength,
                    cancellationToken);
            }
        }

        Task<bool> IFileBackend.SetTagsAsync(object context, string id, Dictionary<string, string> tags, CancellationToken cancellationToken)
        {
            var config = context as Context;
            return config.NextDriver.SetTagsAsync(config.NextDriverContext, id, tags, cancellationToken);
        }

        Task<Dictionary<string, string>> IFileBackend.GetTagsAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;
            return config.NextDriver.GetTagsAsync(config.NextDriverContext, id, cancellationToken);
        }

        Task<FileBackendConstants.OnlineStatus> IFileBackend.GetOnlineStatusAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;
            return config.NextDriver.GetOnlineStatusAsync(config.NextDriverContext, id, cancellationToken);
        }

        Task<bool> IFileBackend.RequestOnlineAsync(object context, string id, CancellationToken cancellationToken)
        {
            var config = context as Context;
            return config.NextDriver.RequestOnlineAsync(config.NextDriverContext, id, cancellationToken);
        }
    }
}