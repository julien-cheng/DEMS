namespace Documents.Clients.Manager.Models.Requests
{
    public class NewPathRequest : ModelBase
    {
        public PathIdentifier PathIdentifier { get; set; }
        public string Name { get; set; }
    }
}
