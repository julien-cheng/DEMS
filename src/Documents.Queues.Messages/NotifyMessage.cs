namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class NotifyMessage
    {
        public UserIdentifier RecipientIdentifier { get; set; }

        public string TemplateName { get; set; }

        public object Model { get; set; }

        public List<FileIdentifier> Attachments { get; set; }
    }
}
