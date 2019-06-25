namespace Documents.Store.SqlServer.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Folder")]
    public class Folder : IHaveAuditDates
    {
        public long FolderID { get; set; }

        [StringLength(64)]
        public string FolderKey { get; set; }

        public Organization Organization { get; set; }
        public long OrganizationID { get; set; }

        public string Metadata { get; set; }

        [ForeignKey("FolderID")]
        public virtual ICollection<Privilege> Privileges { get; set; }

        public virtual ICollection<File> Files { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        [StringLength(64)]
        public string DeletedKey { get; set; }

        [Timestamp]
        public byte[] UpdateVersion { get; set; }
    }
}
