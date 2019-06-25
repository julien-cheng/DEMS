namespace Documents.Backends.Gateway.Controllers
{
    using Documents.Backends.Drivers;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json;
    using Streams;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class BackendController : Controller
    {
        private readonly ILogger<BackendController> Logger;
        private readonly ILogger<IFileBackend> BackendLogger;
        private readonly IMemoryCache MemoryCache;

        IFileBackend backend = null;
        object context;

        public BackendController(
            ILogger<BackendController> logger,
            ILogger<IFileBackend> backendLogger,
            IMemoryCache memoryCache
        )
        {
            Logger = logger;
            BackendLogger = backendLogger;

            MemoryCache = memoryCache;
        }
        
        // Chunk Streams
        [HttpPost, Route("begin")]
        public async Task<string> PostBegin(
            string id,
            [FromBody]FileDetailModel details,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            Logger.LogInformation($"{JsonConvert.SerializeObject(Request.Headers, Formatting.Indented)}");

            Logger.LogInformation($"Content-Type: {details.ContentType}");
            Logger.LogInformation($"File Name: {details.Name}");
            Logger.LogInformation($"File Length: {details.Length}");

            return await backend.ChunkedUploadStartAsync(context, id);
        }

        [HttpPost]
        public async Task<string> PutChunk(
            string id, 
            string uploadKey, 
            string chunkKey, 
            string sequentialState, 
            int chunkIndex, 
            int totalChunks,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogDebug($"SequentialState: {sequentialState}");

            var range = ContentRangeHeaderValue.Parse(Request.Headers["X-Content-Range"]);
            long length = (long)range.To - (long)range.From + 1;

            using (var requestStream = new StreamRequestWrapper(Request.Body, length))
                return await backend.ChunkedUploadChunkAsync(
                    context,
                    id,
                    uploadKey,
                    chunkKey,
                    chunkIndex,
                    totalChunks,
                    sequentialState,
                    (long)range.From,
                    (long)range.To,
                    (long)range.Length,
                    requestStream,
                    cancellationToken
                );
        }

        [HttpPost, Route("end")]
        public async Task<IDictionary<string, object>> PostEnd(
            string uploadKey, 
            string id, 
            [FromBody]ChunkStatusModel[] chunkStatuses, 
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return await backend.ChunkedUploadCompleteAsync(context, uploadKey, id, chunkStatuses, cancellationToken);
        }

        // Download
        [HttpGet]
        public async Task Get(string id, long totalLength, CancellationToken cancellationToken = default(CancellationToken))
        {

            Response.Headers.Add("Accept-Ranges", "bytes");

            var range = RangeHeaderValue.Parse(Request.Headers["Range"]).Ranges.FirstOrDefault();
            if (range == null
                || range.From == null
                || range.To == null)
                throw new Exception("Missing Range Header");

            long from = range.From.Value;
            long to = range.To.Value;

            long contentLength = to - from + 1;
            Response.ContentLength = contentLength;

            Logger.LogInformation($"Get: id: {id} from: {from} to: {to} Content-Length {Response.ContentLength}");
            Response.StatusCode = 206;

            await backend.ReadFileAsync(context, id, Response.Body, from, to, totalLength, cancellationToken).ConfigureAwait(false);

            Logger.LogInformation($"Get: id: {id} complete");
        }

        [HttpDelete]
        public async Task Delete(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            await backend.DeleteFileAsync(context, id, cancellationToken).ConfigureAwait(false);
        }

        [HttpPost, Route("tag")]
        public Task<bool> PostTags(
            string id,
            [FromBody]Dictionary<string, string> tags,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return backend.SetTagsAsync(context, id, tags, cancellationToken);
        }

        [HttpGet, Route("tag")]
        public Task<Dictionary<string, string>> GetTags(
            string id,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return backend.GetTagsAsync(context, id, cancellationToken);
        }

        [HttpPost, Route("online")]
        public Task<bool> PostOnline(
            string id,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return backend.RequestOnlineAsync(context, id, cancellationToken);
        }

        [HttpGet, Route("online")]
        public Task<FileBackendConstants.OnlineStatus> GetOnline(
            string id,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return backend.GetOnlineStatusAsync(context, id, cancellationToken);
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
        {
            var configurationJSON = Request.Headers["X-Backend-Configuration"];
            var backendTypeString = Request.Headers["X-Backend-Driver"];

            var configHash = (configurationJSON + backendTypeString).GetHashCode();
            
            backend = MemoryCache.GetOrCreate(backendTypeString, i => {
                Logger.LogInformation($"Request: {actionContext.ActionDescriptor.DisplayName} "
                    + $"{Request.Path.Value} Driver: {backendTypeString}");

                try
                {
                    var backendType = Type.GetType(backendTypeString);
                    var backend = Activator.CreateInstance(backendType) as IFileBackend;
                    backend.Logger = BackendLogger;
                    return backend;
                }
                catch (Exception e)
                {
                    throw new Exception($"X-Backend-Driver={backendTypeString}: Failed to create backend", e);
                } 
            } );

            this.context = MemoryCache.GetOrCreate(configHash, i => {
                object config = null; 
                
                config = backend.CreateContext(configurationJSON);
                return config;
            });

            await next();
        }
    }
}
