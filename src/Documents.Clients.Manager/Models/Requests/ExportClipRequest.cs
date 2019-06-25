namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models.ViewSets;

    public class ExportClipRequest : ModelBase
    {
        public FileIdentifier FileIdentifier { get; set; }
        public string FileName { get; set; }
        public ClipSegmentModel Clip { get; set; }
    }
}