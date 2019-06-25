namespace Documents.Clients.Manager.Models.Requests
{
    public class AuthenticateUserRequest : ModelBase
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
