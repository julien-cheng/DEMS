namespace Documents.API.Common.Models
{
    using System.Collections.Generic;

    public class PopulationDirective
    {
        public string Name { get; set; }
        public PagingArguments Paging { get; set; } = new PagingArguments
        {
            PageIndex = 0,
            PageSize = 5000
        };

        public List<MetadataMatchModel> MetadataFilter { get; set; }

        public PopulationDirective() { }
        public PopulationDirective(string name)
        {
            this.Name = name;
        }
    }
}