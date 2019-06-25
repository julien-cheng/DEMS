namespace Documents.Clients.Manager.Controllers.ViewSets
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/views/transcriptset")]
    public class TranscriptSetController : ControllerBaseCRUDService<TranscriptSet, FileIdentifier, TranscriptService>
    {
        private readonly ViewSetService ViewSetService;

        public TranscriptSetController(TranscriptService transcriptService, ViewSetService viewSetService)
        {
            Service = transcriptService;
            this.ViewSetService = viewSetService;
        }

        [HttpGet]
        public override Task<TranscriptSet> Get(
            FileIdentifier fileIdentifier,
            CancellationToken cancellationToken
            ) => QueryOneAsync(fileIdentifier, cancellationToken);
    }
}