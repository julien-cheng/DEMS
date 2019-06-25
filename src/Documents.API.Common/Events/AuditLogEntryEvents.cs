namespace Documents.API.Common.Events
{
    public class AuditLogEntryGetEvent : AuditLogEntryEventBase { }
    public class AuditLogEntryPutEvent : AuditLogEntryEventBase { }
    public class AuditLogEntryPostEvent : AuditLogEntryEventBase { }
    public class AuditLogEntryDeleteEvent : AuditLogEntryEventBase { }

    public class AuditLogEntryContentsUploadStartEvent : AuditLogEntryEventBase { }
    public class AuditLogEntryContentsUploadCompleteEvent : AuditLogEntryEventBase { }
    public class AuditLogEntryDownloadEvent : AuditLogEntryEventBase { }

    public class AuditLogEntryTextContentChangeEvent: AuditLogEntryEventBase
    {
        public string Type { get; set; }
    }
}
