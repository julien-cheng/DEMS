namespace Documents.API.Models
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class UserAccessIdentifiersPutRequest
    {
        public UserIdentifier Identifier { get; set; }
        public IEnumerable<string> AccessIdentifiers { get; set; }
    }
}
