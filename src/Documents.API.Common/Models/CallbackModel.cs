namespace Documents.API.Common.Models
{
    public class CallbackModel : IHasIdentifier<FileIdentifier>
    {
        public string Queue { get; set; }
        public string Context { get; set; }
        public string Token { get; set; }
        public FileIdentifier FileIdentifier { get; set; }

        FileIdentifier IHasIdentifier<FileIdentifier>.Identifier {
            get => FileIdentifier;
            set => FileIdentifier = value;
        }
    }
}
