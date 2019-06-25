namespace Documents.Clients.Manager.Models.Requests
{
    public class AddRecipientRequest : RecipientRequestBase
    {
        public AddRecipientDefaults Defaults { get; set; }

        public class AddRecipientDefaults
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }
    }
}
