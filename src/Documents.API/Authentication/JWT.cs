namespace Documents.API.Authentication
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    public class JWT
    {
        private readonly DocumentsAPIConfiguration DocumentsAPIConfiguration;

        public JWT(DocumentsAPIConfiguration documentsAPIConfiguration)
        {
            this.DocumentsAPIConfiguration = documentsAPIConfiguration;
        }

        public string CreateUserToken(UserModel user, string clientClaims = null)
        {
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(SecurityTokenClaimKeys.ORGANIZATION_KEY, user.Identifier.OrganizationKey),
                new Claim(SecurityTokenClaimKeys.SECURITY_IDENTIFIERS, string.Join(" ", user.UserAccessIdentifiers)),
                new Claim(SecurityTokenClaimKeys.USER_KEY, user.Identifier.UserKey),
                new Claim(SecurityTokenClaimKeys.CLIENT_CLAIMS, clientClaims ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Sub, user.Identifier.UserKey),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(now).ToString(), ClaimValueTypes.Integer64)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(DocumentsAPIConfiguration.TokenValidationSecret));

            var expires = TimeSpan.FromSeconds(DocumentsAPIConfiguration.TokenExpirationSeconds);

            var jwt = new JwtSecurityToken(
                issuer: DocumentsAPIConfiguration.TokenIssuer,
                audience: DocumentsAPIConfiguration.TokenAudience,
                claims: claims,
                notBefore: now,
                expires: now.Add(expires),
                signingCredentials: new SigningCredentials(
                    signingKey, SecurityAlgorithms.HmacSha256
                ));

            
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public JwtSecurityToken Read(string token)
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(token);
        }


        /// <summary>
        /// Get this datetime as a Unix epoch timestamp (seconds since Jan 1, 1970, midnight UTC).
        /// </summary>
        /// <param name="date">The date to convert.</param>
        /// <returns>Seconds since Unix epoch.</returns>
        private static long ToUnixEpochDate(DateTime date)
            => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }
}
