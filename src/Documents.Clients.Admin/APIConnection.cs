namespace Documents.Clients.Admin
{
    using Documents.API.Client;
    using Documents.API.Common.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

    public class APIConnection : Connection
    {
        private readonly AdminConfiguration config;
        public APIConnection(
            ILogger<APIConnection> logger, 
            IHttpContextAccessor contextAccessor,
            AdminConfiguration adminConfiguration
        ) : base(new Uri(adminConfiguration.API.Uri))
        {
            this.Logger = logger;
            this.OnSecurityException = this.Login;
            this.config = adminConfiguration;
        }

        private async Task Login()
        {
            var response = await this.User.AuthenticateAsync(new TokenRequestModel
            {
                Identifier = new UserIdentifier(config.API.OrganizationKey, config.API.UserKey),
                Password = config.API.Password
            });

            if (response.Token == null)
                throw new Exception("Security Exception, Admin client could not log in, check configuration.");
        }
    }
}