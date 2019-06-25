namespace Documents.Clients.Manager.Controllers.Views.ViewSets
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models.ViewSets;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [Route("api/views")]
    public class ViewSetController : ManagerControllerBase
    {
        private readonly ViewSetService ViewSetService;
        public ViewSetController(ViewSetService viewSetService)
        {
            this.ViewSetService = viewSetService;
        }

        [HttpGet, Route("documentset")]
        public async Task<DocumentSet> GetDocuments(FileIdentifier fileIdentifier)
        {
            return await this.ViewSetService.LoadSet<DocumentSet>(fileIdentifier);
        }

        [HttpGet, Route("textset")]
        public async Task<TextSet> GetText(FileIdentifier fileIdentifier)
        {
            return await this.ViewSetService.LoadSet<TextSet>(fileIdentifier);
        }

        [HttpGet, Route("mediaset")]
        public async Task<MediaSet> GetMedia(FileIdentifier fileIdentifier)
        {
            return await this.ViewSetService.LoadSet<MediaSet>(fileIdentifier);
        }

        [HttpGet, Route("imageset")]
        public async Task<ImageSet> GetImage(FileIdentifier fileIdentifier)
        {
            return await this.ViewSetService.LoadSet<ImageSet>(fileIdentifier);
        }

        [HttpGet, Route("unknownset")]
        public async Task<UnknownSet> GetUnknown(FileIdentifier fileIdentifier)
        {
            return await this.ViewSetService.LoadSet<UnknownSet>(fileIdentifier);
        }
    }
}