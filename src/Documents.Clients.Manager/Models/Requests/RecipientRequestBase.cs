namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;

    public class RecipientRequestBase : ModelBase
    {
        public FolderIdentifier FolderIdentifier { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RecipientEmail { get; set; }
    }
}
