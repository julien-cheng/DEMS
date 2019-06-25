namespace Documents.Clients.Manager.Controllers.ViewSets
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/views/clipset")]
    public class ClipSetController : ControllerBaseCRUDService<ClipSet, FileIdentifier, ClipService>
    {
        private readonly ViewSetService ViewSetService;

        public ClipSetController(ClipService clipService, ViewSetService viewSetService)
        {
            Service = clipService;
            this.ViewSetService = viewSetService;
        }

        [HttpGet]
        public override Task<ClipSet> Get(
            FileIdentifier fileIdentifier,
            CancellationToken cancellationToken
            ) => QueryOneAsync(fileIdentifier, cancellationToken);

    }
}