namespace Documents.API.Common.Events
{
    using Documents.API.Common.Models;

    public abstract class AuditLogEntryEventBase : EventBase, IHasIdentifier<AuditLogEntryIdentifier>
    {
        public override string Name { get => this.GetType().Name; }

        public AuditLogEntryIdentifier AuditLogEntryIdentifier { get; set; }

        AuditLogEntryIdentifier IHasIdentifier<AuditLogEntryIdentifier>.Identifier
        {
            get => this.AuditLogEntryIdentifier;
            set => this.AuditLogEntryIdentifier = value;
        }

        public override string ToString()
        {
            return $"{Name}:{AuditLogEntryIdentifier?.ToString()}";
        }
    }
}
