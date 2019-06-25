using System;

namespace Documents.Clients.PCMSBridge.Models
{
    public class ChunkedUploadChunk
    {
        public string Token { get; set; }
        public long From { get; set; }
        public long To { get; set; }
        public long Length { get; set; }

        public long ChunkSize { get; set; }
    }
}
