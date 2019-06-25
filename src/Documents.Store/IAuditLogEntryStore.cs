namespace Documents.Store
{
    using Documents.API.Common.Models;

    public interface IAuditLogEntryStore : IModelStore<AuditLogEntryModel, AuditLogEntryIdentifier>
    {
    }
}