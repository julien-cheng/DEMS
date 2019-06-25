namespace Documents.Store.SqlServer.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("File")]
    public class File : IHaveAuditDates
    {
        public long FileID { get; set; }

        [StringLength(64)]
        public string FileKey { get; set; }

        public long FolderID { get; set; }
        public Folder Folder { get; set; }

        [StringLength(200)]
        public string FileLocator { get; set; }

        [StringLength(2000)]
        public string Name { get; set; }

        [StringLength(200)]
        public string MimeType { get; set; }

        [StringLength(50)]
        public string MD5 { get; set; }
        [StringLength(50)]
        public string SHA1 { get; set; }
        [StringLength(50)]
        public string SHA256 { get; set; }


        public long Length { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public FileStatus Status { get; set; }

        public string Metadata { get; set; }

        [ForeignKey("FileID")]
        public virtual ICollection<Privilege> Privileges { get; set; }

        [StringLength(64)]
        public string DeletedKey { get; set; }

        [Timestamp]
        public byte[] UpdateVersion { get; set; }
    }
}
