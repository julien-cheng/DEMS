namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class DownloadRequest : ModelBase
    {
        public FileIdentifier FileIdentifier { get; set; }
        public bool Open { get; set; } = false;
    }
}