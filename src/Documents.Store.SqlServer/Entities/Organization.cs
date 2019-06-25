namespace Documents.Store.SqlServer.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Organization")]
    public class Organization : IHaveAuditDates
    {
        public long OrganizationID { get; set; }

        [StringLength(200)]
        public string OrganizationKey { get; set; }

        [StringLength(200)]
        public string Name { get; set; }

        public string Metadata { get; set; }

        [ForeignKey("OrganizationID")]
        public virtual ICollection<Privilege> Privileges { get; set; }

        public virtual ICollection<User> Users { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        [StringLength(64)]
        public string DeletedKey { get; set; }

        [Timestamp]
        public byte[] UpdateVersion { get; set; }
    }
}
