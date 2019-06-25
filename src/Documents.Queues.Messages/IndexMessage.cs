namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class IndexMessage : FileBasedMessage
    {
        public IndexActions Action { get; set; }
        public FolderModel FolderModel { get; set; }
        public FileModel FileModel { get; set; }
     
        public enum IndexActions
        {
            IndexFile,
            DeleteFile,
            IndexFolder,
            DeleteFolder,
            IndexOrganization,
            DeleteOrganization, 
            DeleteEntireIndex
        }
    }
}
