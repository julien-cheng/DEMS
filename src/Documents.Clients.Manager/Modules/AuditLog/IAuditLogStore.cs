
namespace Documents.Clients.Manager.Modules.AuditLog
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IAuditLogStore
    {
        Task<List<AuditLogEntry>> GetEntries(FolderIdentifier identifier, ModuleType moduleType);
        Task<AuditLogEntry> AddEntry(AuditLogEntry auditLogEntry, FolderIdentifier identifier, APIConnection connectionOverride = null);
        List<AuditLogEntry> TranslateEntriesForDisplay(List<AuditLogEntry> auditLogEntries, string userTimeZone);
    }
}
