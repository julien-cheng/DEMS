namespace Documents.API.Authentication
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.Text;

    public static class JWTServicesExtensions
    {
        public static IServiceCollection UseJWTAuthentication(this IServiceCollection services)
        {

            var sp = services.BuildServiceProvider();
            var config = sp.GetService<DocumentsAPIConfiguration>();

            // API JWT Validation options
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    // The signing key must match!
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config.TokenValidationSecret)),

                    // Validate the JWT Issuer (iss) claim 
                    ValidateIssuer = true,
                    ValidIssuer = config.TokenIssuer,

                    // Validate the JWT Audience (aud) claim
                    ValidateAudience = true,
                    ValidAudience = config.TokenAudience,

                    // Validate the token expiry
                    ValidateLifetime = true,

                    // If you want to allow a certain amount of clock drift, set that here:
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

            return services;
        }
    }
}
