namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;

    public class TranscodeMessage : FileBasedMessage
    {
        public TranscodeMessage() { }
        public TranscodeMessage(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }

        // These should be more describing the kind of file.  For instance, Image, PDF, Screencap etc.
        public string AlternativeViewType { get; set; }

        public string TranscodeConfiguration { get; set; } = "VideoTranscode";
    }
}
