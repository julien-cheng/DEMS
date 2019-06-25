namespace Documents.Clients.Manager.Controllers
{
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [AllowAnonymous]
    [Route("api/configuration")]
    public class ConfigurationController : ManagerControllerBase
    {
        private static int UploadChunkSize = 0;
        private APIConnection APIConnection = null;
        private readonly ManagerConfiguration ManagerConfiguration;

        public ConfigurationController(APIConnection apiConnection, ManagerConfiguration managerConfiguration)
        {
            this.APIConnection = apiConnection;
            this.ManagerConfiguration = managerConfiguration;
        }

        [HttpGet, AllowAnonymous]
        public async Task<ClientConfigurationModel> Configuration()
        {
            if (UploadChunkSize == 0)
                UploadChunkSize = await APIConnection.File.GetUploadChunkSizeAsync(null);

            var config = new ClientConfigurationModel
            {
                MaxChunkSize = UploadChunkSize,
                UserTimeZone = this.APIConnection.UserTimeZone,
                MaxFileSize = ManagerConfiguration.MaxFileSize
            };

            if (APIConnection.ClientClaims == "FullFrame")
                config.IsTopNavigationVisible = true;

            if (EDiscoveryUtility.IsUserEDiscovery(APIConnection.UserAccessIdentifiers))
            {
                config.IsSearchEnabled = false;
            }
            else
            {
                config.IsSearchEnabled = ManagerConfiguration.IsFeatureEnabledSearch;
            }

            return config;
        }
    }
}
