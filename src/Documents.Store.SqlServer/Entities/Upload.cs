namespace Documents.Store.SqlServer.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Upload")]
    public class Upload
    {
        public long UploadID { get; set; }

        public long FileID { get; set; }
        public File File { get; set; }

        [StringLength(2048)]
        public string UploadKey { get; set; }

        public long Length { get; set; }

        public virtual ICollection<UploadChunk> UploadChunks { get; set; }

        public long UserID { get; set; }
        public User User { get; set; }

        public DateTime? Started { get; set; }
    }
}
