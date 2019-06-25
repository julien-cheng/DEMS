namespace Documents.Store.SqlServer.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UploadChunk")]
    public class UploadChunk
    {
        public long UploadChunkID { get; set; }

        public long UploadID { get; set; }
        public Upload Upload { get; set; }

        [StringLength(512)]
        public string ChunkKey { get; set; }

        public int ChunkIndex { get; set; }

        public long PositionFrom { get; set; }
        public long PositionTo { get; set; }

        public bool Success { get; set; }

        [StringLength(4096)]
        public string State { get; set; }
    }
}
