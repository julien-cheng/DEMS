

namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    [Route("api/schema")]
    public class SchemaController : ControllerBase
    {
        private readonly SchemaService schemaService;
        private readonly IOptions<ManagerConfiguration> managerConfiguration;

        public SchemaController(IOptions<ManagerConfiguration> managerConfiguration, SchemaService schemaService)
        {
            this.schemaService = schemaService;
            this.managerConfiguration = managerConfiguration;
        }

        [HttpPost("createSchema")]
        public async Task<FolderModel> CreateSchema([FromBody] FolderIdentifier folderIdentifier)
        {
            return await this.schemaService.CreateSchema(folderIdentifier);
        }
    }
}
