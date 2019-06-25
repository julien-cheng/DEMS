namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class TextExtractMessage : FileBasedMessage
    {
        public TextExtractMessage() { }
        public TextExtractMessage(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }
    }
}
