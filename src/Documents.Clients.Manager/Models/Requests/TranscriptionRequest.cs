namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class TranscriptionRequest : ModelBase
    {
        public FileIdentifier FileIdentifier { get; set; }
    }
}