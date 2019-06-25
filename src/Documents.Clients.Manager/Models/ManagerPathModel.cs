namespace Documents.Clients.Manager.Models
{
    using System.Collections.Generic;

    public class ManagerPathModel : ModelBase, IItemQueryResponse
    {
        public PathIdentifier Identifier { get; set; }
        public string Name { get; set; }

        public string FullPath { get; set; }

        public string[] Icons { get; set; }
        public bool IsExpanded { get; set; }

        public List<ManagerPathModel> Paths { get; set; }

        public IEnumerable<AllowedOperation> AllowedOperations { get; set; }

        public Dictionary<string, object> DataModel { get; set; }
    }
}
