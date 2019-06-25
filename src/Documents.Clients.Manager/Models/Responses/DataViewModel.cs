using System.Collections.Generic;

namespace Documents.Clients.Manager.Models.Responses
{
    public class DataViewModel : IViewModel
    {
        public string Type => "object";
        public ManagerFieldBaseSchema DataSchema { get; set; }
        public object DataModel { get; set; }
        public List<AllowedOperation> AllowedOperations { get; set; }
    }
}
