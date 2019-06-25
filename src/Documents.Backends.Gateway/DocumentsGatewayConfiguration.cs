using Documents.Common;
using Documents.Common.WebHost;

namespace Documents.Backends.Gateway
{
    public class DocumentsGatewayConfiguration : IDocumentsWebConfiguration
    {
        public string HostingURL { get; set; }

        string IDocumentsConfiguration.SectionName => "DocumentsBackendsGateway";
    }
}
