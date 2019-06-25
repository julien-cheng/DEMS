namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class ArchiveMessage : FileBasedMessage
    {
        public ArchiveMessage(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }
    }
}
