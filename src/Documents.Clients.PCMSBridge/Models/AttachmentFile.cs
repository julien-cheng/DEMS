using System;

namespace Documents.Clients.PCMSBridge.Models
{
    public class AttachmentFile
    {
        public string Unique { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PathUnique { get; set; }
        public DateTime Created { get; set; }
        public bool IsGenerated { get; set; }
        public int DefendantID { get; set; }
    }
}
