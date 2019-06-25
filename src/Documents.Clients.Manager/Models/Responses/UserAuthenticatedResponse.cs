using Documents.API.Common.Models;

namespace Documents.Clients.Manager.Models.Responses
{
    public class UserAuthenticatedResponse
    {
        public bool IsAuthenticated { get; set; }
        public FolderIdentifier FolderIdentifier { get; set; }
        public PathIdentifier PathIdentifier { get; set; }
    }
}
