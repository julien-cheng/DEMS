namespace Documents.Clients.Admin
{
    using Documents.Common;
    using Documents.Common.WebHost;

    public class AdminConfiguration : IDocumentsWebConfiguration
    {
        public DocumentsAPIConfiguration API { get; set; }
        public string HostingURL { get; set; } = "http://*:5060";

        string IDocumentsConfiguration.SectionName => "DocumentsClientsAdmin";

        public class DocumentsAPIConfiguration
        {
            public string Uri { get; set; }
            public string OrganizationKey { get; set; }
            public string UserKey { get; set; }
            public string Password { get; set; }
        }
    }
}