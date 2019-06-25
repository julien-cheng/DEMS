namespace Documents.Clients.PCMSBridge
{
    using Documents.Common;
    using Documents.Common.WebHost;

    public class PCMSBridgeConfiguration : IDocumentsWebConfiguration
    {
        public string APIUrl { get; set; }

        public string FinalURL { get; set; }
        public string FinalURLDeepLink { get; set; }
        public string GlobalSearchURL { get; set; }
        public string HandoffURL { get; set; }

        public string HostingURL { get; set; }

        string IDocumentsConfiguration.SectionName => "DocumentsClientsPCMSBridge";
    }
}