namespace Documents.Clients.Manager.Models
{
    using System.Collections.Generic;

    public class SearchConfiguration
    {
        public Dictionary<string, string> LanguageMap { get; set; }
        public List<string> DisplayFields { get; set; }
    }
}
