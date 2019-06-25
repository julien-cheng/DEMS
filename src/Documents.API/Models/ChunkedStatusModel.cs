namespace Documents.API.Models
{
    public class ChunkedStatusModel
    {
        public string UploadChunkKey { get; set; }
        public int ChunkIndex { get; set; }
        public bool Success { get; set; }
        public string State { get; set; }
    }
}
