namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/file")]
    public class FileController : ControllerBaseCRUDService<ManagerFileModel, FileIdentifier, FileService>
    {
        public FileController(FileService fileService)
        {
            Service = fileService;
        }
        
        [HttpGet("contents")]
        public async Task Download(DownloadRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            // we don't want the standard json exception and timing wrapper
            SuppressWrapper = true;

            if (request.FileIdentifier?.IsValid ?? false)
                await Service.FileDownloadAsync(request, Request, Response, cancellationToken);
            else
                throw new ArgumentException(nameof(DownloadRequest.FileIdentifier));
        }

        [HttpPost("saveSchemaData")]
        public async Task<ManagerFileModel> SaveSchemData([FromBody] SaveDataRequest saveDataRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.Service.SaveFormData(saveDataRequest);
        }
    }
}