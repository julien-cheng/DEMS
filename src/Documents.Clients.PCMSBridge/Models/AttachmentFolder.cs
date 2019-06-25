namespace Documents.Clients.PCMSBridge.Models
{
    using System.Collections.Generic;

    public class AttachmentFolder
    {
        public string Unique { get; set; }
        public string Name { get; set; }
        public List<AttachmentFolder> ChildFolders { get; set; }
        public List<AttachmentFile> Attachments { get; set; }
    }
}
