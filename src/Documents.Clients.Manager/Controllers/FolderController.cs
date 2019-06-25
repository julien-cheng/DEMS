namespace Documents.Clients.Manager.Controllers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/folder")]
    public class FolderController : ControllerBaseCRUDService<ManagerFolderModel, FolderIdentifier, FolderService>
    {
        public FolderController(FolderService folderService)
        {
            Service = folderService;
        }

        public Task<IEnumerable<ManagerFolderModel>> List(OrganizationIdentifier organizationIdentifier, CancellationToken cancellationToken)
        {
            return Service.QueryAllAsync(organizationIdentifier, cancellationToken);
        }

        [HttpPost("saveSchemaData")]
        public async Task<ManagerFolderModel> SaveSchemData([FromBody] SaveDataRequest saveDataRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.Service.SaveFormData(saveDataRequest);
        }
    }
}
