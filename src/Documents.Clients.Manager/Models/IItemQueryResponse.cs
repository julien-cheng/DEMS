namespace Documents.Clients.Manager.Models
{
    using System.Collections.Generic;

    public interface IItemQueryResponse
    {
        string Type { get; set; }
        string Name { get; set; }

        IEnumerable<AllowedOperation> AllowedOperations { get; set; }
        Dictionary<string, object> DataModel { get; set; }
    }
}
