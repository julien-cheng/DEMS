namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class FileBasedMessage : IHasIdentifier<FileIdentifier>
    {
        public FileBasedMessage()
        { }
        public FileBasedMessage(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }

        public FileIdentifier Identifier { get; set; }
    }
}
