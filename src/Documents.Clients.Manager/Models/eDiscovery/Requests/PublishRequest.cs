namespace Documents.Clients.Manager.Models.Requests.eDiscovery
{
    using Documents.API.Common.Models;

    class PublishRequest : ModelBase
    {
        public FolderIdentifier FolderIdentifier { get; set; }
        public int eDiscoveryRecipientCount { get; set; }
        public string CustomName { get; set; }
    }
}
