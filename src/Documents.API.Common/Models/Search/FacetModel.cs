namespace Documents.API.Common.Models.Search
{
    using System.Collections.Generic;

    public class FacetModel
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public IEnumerable<FacetValue> Values { get; set; }
    }
}
