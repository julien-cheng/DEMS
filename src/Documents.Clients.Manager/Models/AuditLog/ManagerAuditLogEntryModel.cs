

namespace Documents.Clients.Manager.Models.AuditLog
{
    using System;
    using System.Collections.Generic;

    public class ManagerAuditLogEntryModel :  ModelBase, IItemQueryResponse
    {
        public string Key { get; set; }
        public string UserName { get; set; }
        public string EntryType { get; set; }
        public string Message { get; set; }
        public DateTime? Created { get; set; }

        public IEnumerable<AllowedOperation> AllowedOperations { get; set; }

        public Dictionary<string, object> DataModel { get; set; }
        public string Name { get; set; }
    }
}
