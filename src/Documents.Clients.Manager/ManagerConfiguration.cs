namespace Documents.Clients.Manager
{
    using Documents.API.Common.Models;
    using Documents.Common;
    using Documents.Common.WebHost;

    public class ManagerConfiguration : IDocumentsWebConfiguration
    {
        public APIConfiguration API { get; set; }
        public class APIConfiguration
        {
            public string Uri { get; set; }
        }

        public SSOJWTConfiguration SSOJWT { get; set; }
        public class SSOJWTConfiguration
        {
            public string TokenValidationSecret { get; set; }
            public string TokenIssuer { get; set; }
            public string TokenAudience { get; set; }

            public string ApplicationOrganizationKey { get; set; }
            public string ApplicationUserKey { get; set; }
            public string ApplicationPassword { get; set; }
        }

        public string EDiscoveryLinkEncryptionKey { get; set; }
        public string EDiscoveryLandingLocation { get; set; }

        public string LEOUploadLinkEncryptionKey { get; set; }
        public string LEOUploadLandingLocation { get; set; }

        public string LoginRedirect { get; set; }

        public string EDiscoveryImpersonationOrganization { get; set; }
        public string EDiscoveryImpersonationUser { get; set; }
        public string EDiscoveryImpersonationPasssword { get; set; }

        public string LEOUploadImpersonationOrganization { get; set; }
        public string LEOUploadImpersonationUser { get; set; }
        public string LEOUploadImpersonationPasssword { get; set; }

        public string HostingURL { get; set; }

        public bool IsBackdoorEnabled { get; set; }
        public string BackdoorOrganizationKey { get; set; }
        public string BackdoorUserKey { get; set; }
        public string BackdoorPassword { get; set; }

        public long MaxFileSize { get; set; } = 
            10 * 
            1024 * 1024 * 1024L; // gigs

        public bool IsFeatureEnabledUpload{ get; set; } = true;
        public bool IsFeatureEnabledSearch { get; set; } = true;

        string IDocumentsConfiguration.SectionName => "DocumentsClientsManager";
    }
}
