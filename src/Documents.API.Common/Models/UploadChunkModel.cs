namespace Documents.API.Common.Models
{
    public class UploadChunkModel : IHasIdentifier<UploadChunkIdentifier>
    {
        public UploadChunkIdentifier Identifier { get; set; }

        public int ChunkIndex { get; set; }

        public long PositionFrom { get; set; }
        public long PositionTo { get; set; }

        public bool Success { get; set; }

        public string State { get; set; }
    }
}
