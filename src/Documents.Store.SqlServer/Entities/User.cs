namespace Documents.Store.SqlServer.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("User")]
    public class User : IHaveAuditDates
    {
        public long UserID { get; set; }
        [StringLength(400)]
        public string UserKey { get; set; }

        [StringLength(400)]
        public string EmailAddress { get; set; }

        [StringLength(400)]
        public string FirstName { get; set; }
        [StringLength(400)]
        public string LastName { get; set; }

        [StringLength(400)]
        public string UserSecretHash { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public long OrganizationID { get; set; }
        public Organization Organization { get; set; }

        public virtual ICollection<UserAccessIdentifier> UserAccessIdentifiers { get; set; }

        public virtual ICollection<Upload> Uploads { get; set; }

        [StringLength(64)]
        public string DeletedKey { get; set; }

        [Timestamp]
        public byte[] UpdateVersion { get; set; }

    }
}
