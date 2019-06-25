namespace Documents.Clients.Manager.Models.LogReader
{
    using System;
    using System.Collections.Generic;

    public class LogEntryModel : ModelBase, IItemQueryResponse
    {
        public string Key { get; set; }
        public string Name { get; set; } = "Name";

        public string Action { get; set; }
        public DateTime Generated { get; set; }

        public string Initiator { get; set; }
        public string Description { get; set; }

        public IEnumerable<AllowedOperation> AllowedOperations { get; set; }

        public Dictionary<string, object> DataModel { get; set; }
    }
}
