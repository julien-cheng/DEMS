namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class RenameRequest: ModelBase
    {
        // one of the two of these will be populated, the other null
        public FileIdentifier FileIdentifier { get; set; }
        public PathIdentifier PathIdentifier { get; set; }

        public string NewName { get; set; }
    }
}
