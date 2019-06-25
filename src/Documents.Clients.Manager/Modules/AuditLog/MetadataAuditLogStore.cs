namespace Documents.Clients.Manager.Modules.AuditLog
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MetadataAuditLogStore : IAuditLogStore
    {
        private APIConnection connection;

        public static readonly string AUDIT_LOG_LOCATION = "_AuditLog[Entries]";

        public MetadataAuditLogStore(APIConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>
        /// In nearly all cases except very specific ones, the connection override should remain null. 
        /// </summary>
        /// <param name="auditLogEntry"></param>
        /// <param name="identifier"></param>
        /// <param name="connectionOverride"></param>
        /// <returns></returns>
        public async Task<AuditLogEntry> AddEntry(AuditLogEntry auditLogEntry, FolderIdentifier identifier, APIConnection connectionOverride = null)
        {
            // In some cases we need to be able to pass a special connection in, such is the case for 
            // an eDiscovery logged in user.  This connection needs to come in, because we want to use their credentials, and piggy back off their account.
            // The connection in that case as it's passed in through DI won't have a logged in user, and calls to things like 'open folder' will fail. 
            if(connectionOverride != null)
            {
                this.connection = connectionOverride;
            }

            var currentUserModel = await connection.User.GetAsync(connection.UserIdentifier);

            // This will save us having to set this everywhere it's created.
            if (!auditLogEntry.Created.HasValue)
            {
                auditLogEntry.Created = DateTime.UtcNow;
            }

            if (String.IsNullOrEmpty(auditLogEntry.UserKey))
            {
                auditLogEntry.UserKey = currentUserModel.Identifier.UserKey;
            }

            if (String.IsNullOrEmpty(auditLogEntry.UserName))
            {
                auditLogEntry.UserName = currentUserModel.EmailAddress;
            }

            // Check to see if there's already an audit log started.  This will give me an empty list, if one 
            // doesn't exist, so I'm garunteed this won't result in null. 
            var auditLogMetadataEntries = await this.GetAllEntries(identifier);

            var lastEntry = new AuditLogEntry();
            if (auditLogMetadataEntries.Count > 0)
            {
                lastEntry = auditLogMetadataEntries[auditLogMetadataEntries.Count -1];
            }

            // we're only going to add an audit log entry if there's something different in this entry
            // and the last entry.  The created date will always be different, so we're checking these properties individualy.
            if (lastEntry.Message != auditLogEntry.Message ||
                lastEntry.EntryType != auditLogEntry.EntryType ||
                lastEntry.UserKey != auditLogEntry.UserKey ||
                lastEntry.UserName != auditLogEntry.UserName ||
                lastEntry.ModuleType != auditLogEntry.ModuleType
                )
            {
                auditLogMetadataEntries.Add(auditLogEntry);
                await connection.ConcurrencyRetryBlock(async () =>
                {
                    var folder = await connection.Folder.GetAsync(identifier);

                    folder.Write(AUDIT_LOG_LOCATION, auditLogMetadataEntries);
                    await connection.Folder.PutAsync(folder);
                });

                return auditLogEntry;
            }

            return null;
        }

        public async Task<List<AuditLogEntry>> GetAllEntries(FolderIdentifier identifier)
        {
            var folder = await connection.Folder.GetAsync(identifier);

            var entries = folder.Read<List<AuditLogEntry>>(AUDIT_LOG_LOCATION);

            if(entries == null)
            {
                return new List<AuditLogEntry>();
            }

            return entries;
        }

            public async Task<List<AuditLogEntry>> GetEntries(FolderIdentifier identifier, ModuleType moduleType)
        {
            var folder = await connection.Folder.GetAsync(identifier);

            var entries = folder.Read<List<AuditLogEntry>>(AUDIT_LOG_LOCATION);
            if(entries != null)
            {
                //if(moduleType == ModuleType.eDiscovery)
                //{
                //    return entries.Where(entry => entry.ModuleType == moduleType || entry.ModuleType == null).ToList();
                //}
                return entries.Where(entry => entry.ModuleType == moduleType).OrderByDescending(e => e.Created).ToList();
            }
            return new List<AuditLogEntry>();
        }

        public List<AuditLogEntry> TranslateEntriesForDisplay(List<AuditLogEntry> auditLogEntries, string userTimeZone)
        {
            // we're going to take the list of audit log entries,
            // and translate the date back out to EST.  
            foreach (var auditLogEntry in auditLogEntries)
            {
                auditLogEntry.Created = auditLogEntry.Created.Value.ConvertToLocal(userTimeZone);
            }
            return auditLogEntries;
        }
    }
}
