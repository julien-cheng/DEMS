
namespace Documents.Clients.Manager.Controllers
{
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Exceptions;
    using Documents.Clients.Manager.Models.LEOUpload.Requests;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Route("api/leoupload")]
    public class LEOUploadController : ManagerControllerBase
    {
        private readonly LEOUploadModule leoUpload;
        private readonly ManagerConfiguration managerConfiguration;
        private readonly APIConnection connection;

        public LEOUploadController(ManagerConfiguration managerConfiguration, LEOUploadModule leoUpload, APIConnection connection)
        {
            this.managerConfiguration = managerConfiguration;
            this.leoUpload = leoUpload;
            this.connection = connection;
        }

        [HttpPost("addofficer")]
        public async Task<RecipientResponse> AddOfficer([FromBody] AddOfficerRequest addOfficerRequest)
        {

            var folder = await connection.Folder.GetAsync(addOfficerRequest.FolderIdentifier);

            var recipients = folder.MetaLEOUploadOfficerListRead();
            var recipient = recipients.Where(rec => rec?.Email?.ToLower() == addOfficerRequest.RecipientEmail.ToLower()).FirstOrDefault();
            if (recipient != null)
            {
                throw new RecipientAlreadyPresentException($"You can't add: {addOfficerRequest.RecipientEmail} this officer as they are already present in the list of officers.");
            }

            return await leoUpload.AddRecipientAsync(
                addOfficerRequest,
                managerConfiguration.LEOUploadLandingLocation,
                managerConfiguration.LEOUploadLinkEncryptionKey
            );
        }

        [HttpPost("authenticateuser")]
        public async Task<UserAuthenticatedResponse> AuthenticateUser([FromBody] AuthenticateUserRequest authenticateUserRequest)
        {
            return await leoUpload.AuthenticateUserAsync(authenticateUserRequest, managerConfiguration.LEOUploadLinkEncryptionKey);
        }
    }
}
