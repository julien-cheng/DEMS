namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class RequestOnlineRequest : ModelBase
    {
        public FolderIdentifier FolderIdentifier { get; set; }
    }
}