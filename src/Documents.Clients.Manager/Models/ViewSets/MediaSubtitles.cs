namespace Documents.Clients.Manager.Models
{
    using Documents.API.Common.Models;

    public class MediaSubtitles
    {
        public FileIdentifier FileIdentifier { get; set; }
        public string Label { get; set; } = "English";
        public string Language { get; set; } = "en";
        public bool IsDefault { get; set; }
    }
}
