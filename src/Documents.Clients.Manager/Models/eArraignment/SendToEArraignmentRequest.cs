namespace Documents.Clients.Manager.Models.eArraignment
{
    using Documents.API.Common.Models;

    public class SendToEArraignmentRequest : ModelBase
    {
        public FileIdentifier FileIdentifier { get; set; }
    }
}
