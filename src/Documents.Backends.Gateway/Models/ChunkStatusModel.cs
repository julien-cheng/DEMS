namespace Documents.Backends.Gateway.Models
{
    using Documents.Backends.Drivers;

    public class ChunkStatusModel : IChunkStatus
    {
        public string ChunkKey { get; set; }
        public int ChunkIndex { get; set; }
        public bool Success { get; set; }
        public string State { get; set; }
    }
}