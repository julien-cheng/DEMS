namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class DeleteRequest : ModelBase
    {
        public FileIdentifier FileIdentifier { get; set; }
        public PathIdentifier PathIdentifier { get; set; }
    }
}
