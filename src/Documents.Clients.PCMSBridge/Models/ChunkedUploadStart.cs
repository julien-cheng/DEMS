using System;

namespace Documents.Clients.PCMSBridge.Models
{
    public class ChunkedUploadStart
    {
        public string Name { get; set; }
        public long Length { get; set; }
    }
}
