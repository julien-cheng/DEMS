namespace Documents.Backends.Drivers
{
    public interface IChunkStatus
    {
        string ChunkKey { get; set; }
        int ChunkIndex { get; set; }
        bool Success { get; set; }
        string State { get; set; }
    }
}