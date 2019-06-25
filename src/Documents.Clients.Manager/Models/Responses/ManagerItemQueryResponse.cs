namespace Documents.Clients.Manager.Models.Responses
{
    using System.Collections.Generic;

    public class ItemQueryResponse
    {
        public string PathName { get; set; }
        public IEnumerable<AllowedOperation> AllowedOperations { get; set; }
        public ManagerPathModel PathTree { get; set; }
        public List<PatternRegex> PathNameValidationPatterns { get; set; }
        public List<PatternRegex> FileNameValidationPatterns { get; set; }
        public List<IViewModel> Views { get; set; }
    }
}
