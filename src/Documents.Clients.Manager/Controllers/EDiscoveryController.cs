namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Exceptions;
    using Documents.Clients.Manager.Models.eDiscovery.Responses;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    [Route("api/ediscovery")]
    public class EDiscoveryController : ManagerControllerBase
    {
        private readonly EDiscovery eDiscovery;
        private readonly ManagerConfiguration managerConfiguration;
        private readonly APIConnection connection;

        public EDiscoveryController(ManagerConfiguration managerConfiguration, EDiscovery eDiscovery, APIConnection connection)
        {
            this.managerConfiguration = managerConfiguration;
            this.eDiscovery = eDiscovery;
            this.connection = connection;
        }

        [HttpPost("addrecipient")]
        public async Task<RecipientResponse> AddRecipient([FromBody] AddRecipientRequest addRecipientRequest)
        {

            var folder = await connection.Folder.GetAsync(addRecipientRequest.FolderIdentifier);

            var recipients = folder.MetaEDiscoveryRecipientListRead();
            var recipient = recipients.Where(rec => rec.Email.ToLower() == addRecipientRequest.RecipientEmail.ToLower()).FirstOrDefault();
            if(recipient != null)
            {
                throw new RecipientAlreadyPresentException($"You can't add: {addRecipientRequest.RecipientEmail} this recipient as they are already present in the list of recipients.");
            }

            return await eDiscovery.AddRecipientAsync(
                addRecipientRequest, 
                managerConfiguration.EDiscoveryLandingLocation, 
                managerConfiguration.EDiscoveryLinkEncryptionKey
            );
        }

        [HttpGet("stats")]
        public async Task<EDiscoveryStatisticsResponse> EDiscoveryStatisticsAsync(FolderIdentifier folderIdentifier)
        {
            return await eDiscovery.GetEDiscoveryStatistics(folderIdentifier);
        }

        [HttpPost("authenticateuser")]
        public async Task<UserAuthenticatedResponse> AuthenticateUser([FromBody] AuthenticateUserRequest authenticateUserRequest)
        {
            return await eDiscovery.AuthenticateUserAsync(authenticateUserRequest, managerConfiguration.EDiscoveryLinkEncryptionKey);
        }
    }
}
 