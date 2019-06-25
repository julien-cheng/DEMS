namespace Documents.Clients.Manager.Models.Upload
{
    public class BrowserFileInformation
    {
        public long? LastModified { get; set; }
        public string LastModifiedDate { get; set; }
        public string Name { get; set; }
        public long? Size { get; set; }
        public string Type { get; set; }
        public string WebkitRelativePath { get; set; }
        public string FullPath { get; set; }
    }
}
