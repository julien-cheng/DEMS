namespace Documents.API.Common.Models
{
    public class UploadContextModel
    {
        public string UploadToken { get; set; }
        public string SequentialState { get; set; }
        public long FileLength { get; set; }
        public int TotalChunks { get; set; }
        public int ChunkSize { get; set; }
    }
}
