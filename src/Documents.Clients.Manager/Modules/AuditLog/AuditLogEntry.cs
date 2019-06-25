namespace Documents.Clients.Manager.Modules.AuditLog
{
    using Documents.Clients.Manager.Models;
    using System;

    public enum AuditLogEntryType
    {
        eDiscoveryUserLogin = 1,
        eDiscoveryUserPackageAccess = 2,
        eDiscoveryPackageCreated = 3,
        eDiscoveryRecipientAdded = 4,
        eDiscoveryRecipientRegenerated = 5,
        eDiscoveryRecipientDeleted = 6,

        LEOUploadUserLogin = 7,
        LEOUploadOfficerAdded = 10,
        LEOUploadOfficerRegenerated = 11,
        LEOUploadOfficerDeleted = 12,
        LEOUploadOfficerUpload = 13,
    }

    public class AuditLogEntry: ModelBase
    {
        public string UserKey { get; set; }
        public string UserName { get; set; }
        public AuditLogEntryType EntryType { get; set; }
        public string Message { get; set; }
        public DateTime? Created { get; set; }
        public ModuleType ModuleType { get; set; } = ModuleType.eDiscovery;

        public static string GetTitleForEntryType(AuditLogEntryType entryType)
        {
            switch (entryType)
            {
                case AuditLogEntryType.eDiscoveryUserLogin:
                    return "eDiscovery User Login";
                case AuditLogEntryType.eDiscoveryUserPackageAccess:
                    return "eDiscovery Package Access";
                case AuditLogEntryType.eDiscoveryPackageCreated:
                    return "eDiscovery Package Created";
                case AuditLogEntryType.eDiscoveryRecipientAdded:
                    return "eDiscovery Recipient Added";
                case AuditLogEntryType.eDiscoveryRecipientRegenerated:
                    return "eDiscovery Recipient Password Regenerated";
                case AuditLogEntryType.eDiscoveryRecipientDeleted:
                    return "eDiscovery Recipient Deleted";
                case AuditLogEntryType.LEOUploadOfficerAdded:
                    return "LEO Officer Added";
                case AuditLogEntryType.LEOUploadOfficerDeleted:
                    return "LEO Officer Deleted";
                case AuditLogEntryType.LEOUploadOfficerRegenerated:
                    return "LEO Officer had their Password regenerated";
                case AuditLogEntryType.LEOUploadUserLogin:
                    return "LEO Officer Logged In.";
                case AuditLogEntryType.LEOUploadOfficerUpload:
                    return "LEO Officer Upload";
                default:
                    return "No Mapping for entry type";
            }
        }
    }
}
