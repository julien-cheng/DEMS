namespace Documents.Clients.Manager.Common
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;

    public class SSOJWTAuthentication
    {
        private readonly ManagerConfiguration ManagerConfiguration;

        // NYPTI's SSO system was originally SOAP-based
        // dotnet core didn't support SOAP-auth headers, hence an intermediate
        // JWT "service" was created which would receive the SOAP-style authentication
        // and hand it off to a JWT style service.  this decodes that JWT.
        public SSOJWTAuthentication(ManagerConfiguration managerConfiguration)
        {
            this.ManagerConfiguration = managerConfiguration;
        }

        public ClaimsPrincipal Authenticate(string jwt)
        {
            if (ManagerConfiguration?.SSOJWT?.TokenValidationSecret == null)
                throw new Exception("Configuration error, missing SSOJWT TokenValidationSecret");

            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                ManagerConfiguration.SSOJWT.TokenValidationSecret
            ));

            var tokenValidationParameters = new TokenValidationParameters
            {
                // The signing key must match!
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                // Validate the JWT Issuer (iss) claim
                ValidateIssuer = true,
                ValidIssuer = ManagerConfiguration.SSOJWT.TokenIssuer,

                // Validate the JWT Audience (aud) claim
                ValidateAudience = true,
                ValidAudience = ManagerConfiguration.SSOJWT.TokenAudience,

                // Validate the token expiry
                ValidateLifetime = false,

                // If you want to allow a certain amount of clock drift, set that here:
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();

            return handler.ValidateToken(jwt, tokenValidationParameters, out SecurityToken validatedToken);
        }
    }
}