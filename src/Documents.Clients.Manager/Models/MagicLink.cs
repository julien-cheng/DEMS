namespace Documents.Clients.Manager.Models.eDiscovery
{
    using Documents.API.Common.Models;
    using System;

    public class MagicLink
    {
        public string RecipientEmail { get; set; }
        public DateTime ExipirationDate { get; set; }
        public FolderIdentifier FolderIdentifier { get; set; }
        public UserIdentifier UserIdentifier { get; set; }

        // NOTE: from here down, are doomed fields. They exist only to bridge a backwards compatibility gap
        [Obsolete]
        public EDiscoveryUser User { get; set; }
        [Obsolete]
        public string OrganizationKey { get; set; }
        [Obsolete]
        public string FolderKey { get; set; }

        [Obsolete]
        public class EDiscoveryUser
        {
            public string UserKey { get; set; }
            public string EmailAddress { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}
