namespace Documents.Clients.Manager.Models
{
    using Documents.API.Common.Models;

    public class MediaSource
    {
        public MediaSource() { }
        public MediaSource(FileIdentifier fileIdentifier, string type)
        {
            this.FileIdentifier = fileIdentifier;
            this.Type = type;
        }

        public FileIdentifier FileIdentifier { get; set; }
        public string Type { get; set; }
        
    }
}
