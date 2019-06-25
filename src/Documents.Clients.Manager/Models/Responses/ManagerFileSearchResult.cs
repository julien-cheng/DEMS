namespace Documents.Clients.Manager.Models.Responses
{
    using System.Collections.Generic;

    public class ManagerFileSearchResult : ManagerFileModel
    {
        public IEnumerable<string> Highlights { get; set; }
        public string FullPath { get; set; }
    }
}
