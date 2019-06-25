namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class TranscribeMessage : FileBasedMessage
    {
        public TranscribeMessage() { }
        public TranscribeMessage(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }


        public UserIdentifier RequestedBy { get; set; }
    }
}
