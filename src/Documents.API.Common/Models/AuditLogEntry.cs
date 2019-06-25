namespace Documents.API.Common.Models
{
    using System;

    public class AuditLogEntryModel : IHasIdentifier<AuditLogEntryIdentifier>
    {
        public AuditLogEntryIdentifier Identifier { get; set; }

        public UserIdentifier InitiatorUserIdentifier { get; set; }
        public string ActionType { get; set; }
        public string UserAgent { get; set; }
        public DateTime Generated { get; set; }

        public UserIdentifier UserIdentifier { get; set; }
        public OrganizationIdentifier OrganizationIdentifier { get; set; }
        public FolderIdentifier FolderIdentifier { get; set; }
        public FileIdentifier FileIdentifier { get; set; }

        public string Description { get; set; }
        public string Details { get; set; }
    }
}
