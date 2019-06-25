namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class ExportFrameRequest : ModelBase
    {
        public FileIdentifier FileIdentifier { get; set; }
        public string FileName { get; set; }
        public int Milliseconds { get; set; }
    }
}