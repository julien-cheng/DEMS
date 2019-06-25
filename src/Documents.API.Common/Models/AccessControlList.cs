namespace Documents.API.Common.Models
{
    using System.Collections.Generic;

    public class ACLModel
    {
        public ACLModel() { }
        public ACLModel(params string[] identifiers)
        {
            this.RequiredIdentifiers = identifiers;
        }

        public string OverrideKey { get; set; } = "default";

        public IEnumerable<string> RequiredIdentifiers { get; set; }
    }
}
