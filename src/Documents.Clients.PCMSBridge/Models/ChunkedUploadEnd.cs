using System;

namespace Documents.Clients.PCMSBridge.Models
{
    public class UploadCommit
    {
        public string FileUnique { get; set; }
        public string PathUnique { get; set; }
        public string Description { get; set; }
    }
}
