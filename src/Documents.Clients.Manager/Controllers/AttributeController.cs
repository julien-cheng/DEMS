

namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    [Route("api/attribute")]
    public class AttributeController : ManagerControllerBase
    {
        private readonly AttributeService attributeService;
        private readonly IOptions<ManagerConfiguration> managerConfiguration;

        public AttributeController(IOptions<ManagerConfiguration> managerConfiguration, AttributeService attributeService)
        {
            this.attributeService = attributeService;
            this.managerConfiguration = managerConfiguration;
        }

        [HttpPost("createAttributeLocators")]
        public async Task<List<AttributeLocator>> CreateAttributeLocators([FromBody] FolderIdentifier folderIdentifier)
        {
           return await this.attributeService.CreateAttributeLocators(folderIdentifier);
        }

        [HttpPost("createFileAttributes")]
        public async Task<FileModel> CreateAttributesForFileAsync([FromBody] FileIdentifier fileIdentifier)
        {
            return await this.attributeService.CreateAttributesForFileAsync(fileIdentifier);
        }

        [HttpPost("createFolderAttributes")]
        public async Task<FolderModel> CreateFolderAttributesAsync([FromBody] FolderIdentifier folderIdentifier)
        {
            return await this.attributeService.CreateFolderAttributesAsync(folderIdentifier);
        }


        [HttpDelete("clearFileAttributes")]
        public async Task<FileModel> ClearFileAttributesAsync([FromBody] FileIdentifier fileIdentifier)
        {
            return await this.attributeService.ClearFileAttributesAsync(fileIdentifier);
        }
    }
}
