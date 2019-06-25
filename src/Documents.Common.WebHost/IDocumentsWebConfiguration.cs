namespace Documents.Common.WebHost
{
    public interface IDocumentsWebConfiguration : IDocumentsConfiguration
    {
        string HostingURL { get; set; }
    }
}
