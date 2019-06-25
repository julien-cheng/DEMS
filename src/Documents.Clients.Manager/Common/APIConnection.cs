namespace Documents.Clients.Manager.Common
{
    using Documents.API.Client;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System;

    public class APIConnection : Connection
    {
        // this class serves as a DI wrapper for the Connection class
        // it 
        //      pushes the cookie-based token into a transient DI instance
        //      configures the URL
        //      sets up a DI sourced logger

        private static string JWT_TOKEN_LOCATION = "jwt.api";

        private readonly IHttpContextAccessor httpContextAccessor;

        public APIConnection(
            ILogger<APIConnection> logger, 
            IHttpContextAccessor contextAccessor,
            ManagerConfiguration managerConfiguration
        ) : base(new Uri(managerConfiguration.API.Uri))
        {
            this.Logger = logger;
            this.Token = contextAccessor.HttpContext.Request.Cookies[JWT_TOKEN_LOCATION];
            this.httpContextAccessor = contextAccessor;
        }

        public void AddCookieTokenToResponse()
        {
            this.httpContextAccessor.HttpContext.Response.Cookies.Append(JWT_TOKEN_LOCATION, this.Token, new CookieOptions
            {
                HttpOnly = true
            });
        }
    }
}