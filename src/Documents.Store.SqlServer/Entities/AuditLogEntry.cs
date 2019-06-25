namespace Documents.Store.SqlServer.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("AuditLogEntry")]
    public class AuditLogEntry
    {
        public long AuditLogEntryID { get; set; }

        [StringLength(64)]
        public string ActionType { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [StringLength(200)]
        public string InitiatorOrganizationKey { get; set; }
        [StringLength(400)]
        public string InitiatorUserKey { get; set; }
        
        [StringLength(400)]
        public string UserAgent { get; set; }

        [StringLength(200)]
        public string OrganizationKey { get; set; }
        [StringLength(64)]
        public string FolderKey { get; set; }
        [StringLength(64)]
        public string FileKey { get; set; }
        [StringLength(64)]
        public string UserKey { get; set; }

        public string Details { get; set; }

        public DateTime Generated { get; set; }
    }
}
