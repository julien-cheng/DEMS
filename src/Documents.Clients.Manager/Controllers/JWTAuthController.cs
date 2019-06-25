namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;

    [AllowAnonymous]
    public class JWTAuthController : Controller
    {
        private readonly ManagerConfiguration ManagerConfiguration;
        private readonly APIConnection Connection;
        private readonly SSOJWTAuthentication SSOJWTAuthentication;
        private readonly ILogger<JWTAuthController> Logger;

        public JWTAuthController(
            ManagerConfiguration managerConfiguration,
            APIConnection connection,
            SSOJWTAuthentication ssoJWTAuthentication,
            ILogger<JWTAuthController> logger
        )
        {
            ManagerConfiguration = managerConfiguration;
            Connection = connection;
            SSOJWTAuthentication = ssoJWTAuthentication;
            Logger = logger;
        }
        
        public IActionResult RedirectToLogin()
        {
            return Redirect(ManagerConfiguration.LoginRedirect);
        }

        public async Task<IActionResult> Backdoor()
        {
            if (ManagerConfiguration.IsBackdoorEnabled)
            {
                await Connection.User.AuthenticateAsync(new TokenRequestModel
                {
                    Identifier = new UserIdentifier
                    {
                        OrganizationKey = ManagerConfiguration.BackdoorOrganizationKey,
                        UserKey = ManagerConfiguration.BackdoorUserKey
                    },
                    Password = ManagerConfiguration.BackdoorPassword,
                    ClientClaims = "FullFrame"
                });

                Connection.AddCookieTokenToResponse();

                return Redirect($"/case-list/{HttpUtility.UrlEncode(Connection.UserIdentifier.OrganizationKey)}");
            }
            return BadRequest();
        }

        [HttpPost]
        public IActionResult TokenFromPost(string token, string redirect)
        {
            // This method is used as a means of doing a secure redirection
            // from an application that has already generated a JWT for
            // the user
            Response.Cookies.Append("jwt.api", token, new CookieOptions
            {
                HttpOnly = true
            });
            return Redirect(redirect);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string DL, string jwt)
        {
            // this method is from direct entry from the NYPTI SSO system.
            // it authenticates with a system account, then creates/logs-into 
            // the user based on trusted credentials from the SSO system.
            try
            {
                var ssoClaimsPrincipal = SSOJWTAuthentication.Authenticate(jwt);

                var uid = ssoClaimsPrincipal.Claims.FirstOrDefault(c => c.Type == "uid").Value;
                var email = ssoClaimsPrincipal.Claims.FirstOrDefault(c => c.Type == "eml").Value;

                await Connection.User.AuthenticateAsync(new TokenRequestModel
                {
                    Identifier = new UserIdentifier
                    {
                        OrganizationKey = ManagerConfiguration.SSOJWT.ApplicationOrganizationKey,
                        UserKey = ManagerConfiguration.SSOJWT.ApplicationUserKey
                    },
                    Password = ManagerConfiguration.SSOJWT.ApplicationPassword,
                    ClientClaims = "FullFrame"
                });

                var user = await Connection.User.PutAsync(new UserModel
                {
                    Identifier = new UserIdentifier
                    {
                        OrganizationKey = Connection.UserIdentifier.OrganizationKey,
                        UserKey = email
                    },
                    EmailAddress = email
                });

                await Connection.User.AccessIdentifiersPutAsync(user.Identifier, new[] {
                    "o:dany",
                    "x:pcms",
                    "x:eDiscovery",
                    "x:leo",
                    "g:DocumentsDeleteGenerated"
                });
                await Connection.User.ImpersonateAsync(user.Identifier);

                Connection.AddCookieTokenToResponse();
                return Redirect($"/case-list/{HttpUtility.UrlEncode(Connection.UserIdentifier.OrganizationKey)}");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception in JWTAuth/Index");
                throw;
            }
        }
    }
}
