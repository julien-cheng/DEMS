namespace Documents.Clients.Manager.Controllers
{
    using Common;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models.Upload;
    using Documents.Clients.Manager.Modules;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class UploadController : Controller
    {
        private readonly ILogger Logger;
        private readonly APIConnection Connection;
        private readonly ModuleConfigurator ModuleConfigurator;
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public UploadController(
            ILogger<UploadController> logger,
            ModuleConfigurator moduleConfigurator,
            APIConnection connection
        )
        {
            Logger = logger;
            ModuleConfigurator = moduleConfigurator;
            Connection = connection;
        }
        
        [HttpPost, DisableFormValueModelBinding]
        public async Task<ActionResult> UploadChunk(
            PathIdentifier pathIdentifier,
            BrowserFileInformation fileInformation,
            string token,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (!Request.ContentLength.HasValue)
                throw new Exception("Missing Content-Length header");
            if (Request.Headers["Content-Range"].Count == 0)
                throw new Exception("Missing Content-Range header");
            if (Request.Headers["Content-Disposition"].Count == 0)
                throw new Exception("Missing Content-Disposition header");

            var range = ContentRangeHeaderValue.Parse(Request.Headers["Content-Range"]);

            if (!range.HasLength)
                throw new Exception("Content-Range header does not include total length");

            long from = (long)range.From;
            long to = (long)range.To;
            long fileLength = (long)range.Length;

            var fileName = fileInformation?.Name;

            if (fileName == null)
            {
                var contentDisposition = ContentDispositionHeaderValue.Parse(Request.Headers["Content-Disposition"]);
                fileName = contentDisposition?.FileName;
                if (fileName == null)
                    throw new Exception("Filename is not specified in either Content-Disposition header or fileInformation");

                // for some dumb reason, the ContentDispositionHeaderValue parser doesn't finish parsing file names
                // it leaves them quoted
                if (fileName.StartsWith('"') && fileName.EndsWith('"') && fileName.Length > 1)
                    fileName = WebUtility.UrlDecode(fileName.Substring(1, fileName.Length - 2));
            }

            Stream stream = Request.Body;
            UploadContextModel tokenState = null;

            // test retries
            //if ((new Random()).NextDouble() < .3)
            //    throw new Exception("Chaos Monkey");

            bool isFirstChunk = from == 0;
            bool isLastChunk = to == (fileLength - 1);

            FileIdentifier fileIdentifier = null;

            var folderModel = await Connection.Folder.GetOrThrowAsync(pathIdentifier);
            var modules = ModuleConfigurator.GetActiveModules(folderModel);

            // === Step 1: Begin Upload
            if (isFirstChunk)
            {
                var fileModel = new FileModel
                {
                    Identifier = new FileIdentifier(pathIdentifier, null),
                    Name = fileName,
                    Modified = fileInformation?.LastModified != null
                        ? epoch.AddMilliseconds(fileInformation.LastModified.Value)
                        : DateTime.UtcNow,
                    Length = fileLength,
                    MimeType = fileInformation?.Type ?? Request.ContentType ?? "application/octet-stream",
                }.InitializeEmptyMetadata();

                fileModel.Created = new DateTime(Math.Min(DateTime.UtcNow.Ticks, fileModel.Modified.Ticks),DateTimeKind.Utc);

                // some browsers will send us the relative path of a file during a folder upload
                var relativePath = fileInformation?.FullPath;
                if (!string.IsNullOrWhiteSpace(relativePath) && relativePath != "/")
                {
                    if (relativePath.StartsWith("/"))
                        relativePath = relativePath.Substring(1);

                    pathIdentifier = pathIdentifier.CreateChild(relativePath);
                }

                fileModel.MetaPathIdentifierWrite(pathIdentifier);

                foreach (var module in modules)
                    await module.PreUploadAsync(folderModel, fileModel);

                tokenState = await Connection.File.UploadBeginAsync(fileModel, cancellationToken: cancellationToken);
            }
            else
            {
                if (token == null)
                    throw new Exception("Uploaded secondary chunk without token");

                tokenState = JsonConvert.DeserializeObject<UploadContextModel>(token);
            }


            // === Step 2: Send this Chunk
            using (stream)
            {
                if (!isLastChunk)
                {
                    if (to - from != tokenState.ChunkSize - 1)
                        throw new Exception($"Chunk Size Mismatch: received ({to - from}) expected ({tokenState.ChunkSize})");
                }

                int chunkIndex = (int)(from / tokenState.ChunkSize);

                tokenState = await Connection.File.UploadSendChunkAsync(
                    tokenState,
                    chunkIndex,
                    from,
                    to,
                    stream,
                    cancellationToken: cancellationToken
                );
            }

            // === Step 3: End the Upload
            if (isLastChunk)  // if file is one chunk, all three steps happen in a single call
            {
                var fileModel = await Connection.File.UploadEndAsync(tokenState, cancellationToken: cancellationToken);
                fileIdentifier = fileModel.Identifier;

                foreach (var module in modules)
                    await module.PostUploadAsync(folderModel, fileModel);
            }

            return Json(new APIResponse<UploadedFileResponse>
            {
                Response = new UploadedFileResponse
                {
                    FileIdentifier = fileIdentifier,
                    Token = JsonConvert.SerializeObject(tokenState)
                }
            });
        }
    }
}