namespace Documents.API.Common.Models
{
    public class TokenResponseModel
    {
        public string Token { get; set; }
        public OrganizationModel Organization { get; set; }
        public UserModel User { get; set; }
    }
}
