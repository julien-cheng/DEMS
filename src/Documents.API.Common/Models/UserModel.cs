namespace Documents.API.Common.Models
{
    using System.Collections.Generic;

    public class UserModel : IHasIdentifier<UserIdentifier>, IProvideETag
    {
        public UserModel() { }
        public UserModel(UserIdentifier identifier)
        {
            this.Identifier = identifier;
        }

        public UserIdentifier Identifier { get; set; }
        public string EmailAddress { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public IEnumerable<string> UserAccessIdentifiers { get; set; }

        string IProvideETag.ETag { get; set; }

    }
}
