namespace Documents.API.Models
{
    using Documents.API.Common.Models;

    public class UserPasswordPutRequest
    {
        public UserIdentifier Identifier { get; set; }
        public string Password { get; set; }
    }
}
