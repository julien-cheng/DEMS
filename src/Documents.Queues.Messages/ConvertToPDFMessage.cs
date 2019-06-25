namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class ConvertToPDFMessage : FileBasedMessage
    {
        public ConvertToPDFMessage() { }
        public ConvertToPDFMessage(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }
    }
}
