namespace Documents.Clients.PCMSBridge.Common
{
    using System;
    using Documents.API.Client;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    public class APIConnection : Connection
    {
        public APIConnection(
            ILogger<APIConnection> logger, 
            IHttpContextAccessor contextAccessor,
            PCMSBridgeConfiguration pcmsBridgeConfiguration
        ) : base(new Uri(pcmsBridgeConfiguration.APIUrl))
        {
            this.Logger = logger;
            this.Token = contextAccessor.HttpContext.Request.Cookies["jwt.api"];
        }
    }
}