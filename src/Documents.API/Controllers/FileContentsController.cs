namespace Documents.API.Controllers
{
    using Common.Models;
    using Documents.API.Common;
    using Documents.API.Exceptions;
    using Documents.API.Services;
    using Documents.Store;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class FileContentsController : APIControllerBase
    {
        private readonly FileContentsService FileContentsService;
        private readonly IFileStore FileStore;

        public FileContentsController(
            IFileStore fileStore,
            FileContentsService fileContentsService,
            ILogger<FileContentsController> logger,
            ISecurityContext securityContext
        ) : base(securityContext)

        {
            this.FileStore = fileStore;
            this.FileContentsService = fileContentsService;
        }

        [HttpGet]
        public async Task<StreamingResult> GetContents(FileIdentifier identifier, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                throw new ModelValidationException(ModelState);
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));
            if (!identifier.IsValid)
                throw new InvalidIdentifierException();

            var fileModel = await FileStore.GetOneAsync(identifier);
            if (fileModel == null)
                throw new ObjectDoesNotExistException();

            long from = 0;
            long to = fileModel.Length - 1;

            if (Request.Headers["Range"].Any())
            {
                var range = RangeHeaderValue.Parse(Request.Headers["Range"].ToString()).Ranges.FirstOrDefault();
                if (range.From.HasValue)
                    from = range.From.Value;
                if (range.To.HasValue)
                    to = Math.Min(range.To.Value, to);

                Response.StatusCode = (int)HttpStatusCode.PartialContent;
                Response.Headers.Add("Content-Range", new ContentRangeHeaderValue(
                    from,
                    to,
                    fileModel.Length
                ).ToString());
            }

            long rangeLength = to - from + 1;

            var contentDisposition = new ContentDispositionHeaderValue("attachment");
            contentDisposition.SetHttpFileName(fileModel.Name);
            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

            // tell the base class not to screw with our result
            SuppressWrapper = true;
            return new StreamingResult(async response =>
            {
                Response.ContentLength = rangeLength;
                Response.ContentType = fileModel.MimeType ?? "application/octet-stream";

                await FileContentsService.DownloadAsync(fileModel, Response.Body, from, to, cancellationToken);
            });
        }

        [HttpPost, Route("begin")]
        public async Task<UploadContextModel> PostBegin(
            [FromBody] FileModel fileModel
        )
        {
            if (fileModel == null)
                throw new ArgumentNullException(nameof(fileModel));

            return await FileContentsService.UploadBeginAsync(fileModel);
        }

        [HttpGet, Route("chunksize"), AllowAnonymous]
        public Task<int> GetChunkSize(OrganizationIdentifier identifier, CancellationToken cancellationToken)
        {
            return FileContentsService.UploadChunkSizeGetAsync();
        }

        [HttpPatch]
        public async Task<UploadContextModel> PatchRange(
            UploadContextModel uploadContext,
            int chunkIndex,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (Request.Headers["Content-Range"].Count == 0)
                throw new Exception("Missing required Content-Range header");
            var range = ContentRangeHeaderValue.Parse(Request.Headers["Content-Range"].ToString());
            if (range.From == null)
                throw new ArgumentNullException("Range Header From must be specified");
            if (range.To == null)
                throw new ArgumentNullException("Range Header To must be specified");
            if ((long)range.Length != uploadContext.FileLength)
                throw new ArgumentNullException("Range Header Length does not match UploadContext.FileLength");


            return await FileContentsService.UploadChunkAsync(
                uploadContext, (long)range.From, (long)range.To, Request.Body, chunkIndex, cancellationToken);
        }

        [HttpPost, Route("end")]
        public async Task<FileModel> PostEnd([FromBody]UploadContextModel uploadContext)
        {
            return await FileContentsService.UploadCompleteAsync(uploadContext);
        }

        [HttpPost, Route("tag")]
        public Task<bool> PostTags(
            FileIdentifier identifier,
            [FromBody] Dictionary<string, string> tags,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return FileContentsService.SetTagsAsync(identifier, tags, cancellationToken);
        }

        [HttpGet, Route("tag")]
        public Task<Dictionary<string, string>> GetTags(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return FileContentsService.GetTagsAsync(identifier, cancellationToken);
        }

        [HttpPost, Route("online")]
        public Task<bool> PostOnline(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return FileContentsService.RequestOnlineAsync(identifier, cancellationToken);
        }

        [HttpGet, Route("online")]
        public async Task<FileModel.OnlineStatus> GetOnline(
            FileIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var status = await FileContentsService.GetOnlineStatusAsync(identifier, cancellationToken);

            switch (status)
            {
                case FileBackendConstants.OnlineStatus.Offline:
                    return FileModel.OnlineStatus.Offline;
                case FileBackendConstants.OnlineStatus.Restoring:
                    return FileModel.OnlineStatus.Restoring;
                case FileBackendConstants.OnlineStatus.Online:
                    return FileModel.OnlineStatus.Online;
            }

            throw new Exception("Unexpected online status");
        }
    }
}
