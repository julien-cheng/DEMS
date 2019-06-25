namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.API.Common.Security;
    using Documents.API.Queue;
    using Documents.API.Services;
    using Documents.Store;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System.Net;
    using System.Threading.Tasks;

    [Route("/api/v1/callback")]
    public class CallbackController : APIControllerBase
    {
        private readonly DocumentsAPIConfiguration DocumentsAPIConfiguration;
        private readonly QueueSender QueueSender;
        private readonly IFileStore FileStore;
        private readonly FileContentsService FileContentsService;
        private readonly ISecurityContext SecurityContext;

        public CallbackController(
            DocumentsAPIConfiguration documentsAPIConfiguration,
            QueueSender queueSender,
            IFileStore fileStore,
            FileContentsService fileContentsService,
            ISecurityContext securityContext
        ) : base(securityContext)
        {
            this.DocumentsAPIConfiguration = documentsAPIConfiguration;
            this.QueueSender = queueSender;
            this.FileStore = fileStore;
            this.FileContentsService = fileContentsService;
            this.SecurityContext = securityContext;
        }

        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> Post(string token)
        {
            var decrypted = TokenEncryption.Decrypt(token, DocumentsAPIConfiguration.APITokenKey);
            
            var callback = JsonConvert.DeserializeObject<CallbackModel>(decrypted);

            if (callback.FileIdentifier != null
                && Request.ContentLength.HasValue
                && Request.ContentLength.Value > 0
            )
            {
                SecurityContext.AssumeToken(callback.Token);

                var file = await FileStore.GetOneAsync(callback.FileIdentifier) ?? new FileModel(callback.FileIdentifier);
                file.Length = Request.ContentLength.Value;
                file.MimeType = Request.ContentType ?? "application/octet-stream";
                file.Name = "callback";
                file = await FileContentsService.UploadEntireFileAsync(file, Request.Body);
            }

            await QueueSender.SendAsync(callback.Queue, JsonConvert.SerializeObject(callback));
            
            SuppressWrapper = true;
            return Ok();
        }

        [HttpPost, Route("create")]
        public string CreateCallback([FromBody]CallbackModel callback)
        {
            var token = TokenEncryption.Encrypt(
                JsonConvert.SerializeObject(callback),
                DocumentsAPIConfiguration.APITokenKey
            );

            return string.Format(DocumentsAPIConfiguration.CallbackURL, WebUtility.UrlEncode(token));
        }
    }
}
