namespace Documents.Store.Utilities
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;
    using System.Linq;

    public static class PopulationDirectiveExtensions
    {
        public static IEnumerable<PopulationDirective> Prune(this IEnumerable<PopulationDirective> populateRelationships, string name)
        {
            var nextSet = new List<PopulationDirective>();

            if (populateRelationships != null)
                foreach (var directive in populateRelationships)
                {
                    var parts = directive.Name.Split('.');
                    if (parts.Length > 1 && parts[0] == name)
                        nextSet.Add(new PopulationDirective
                        {
                            Name = string.Join('.', parts.Skip(1)),
                            MetadataFilter = directive.MetadataFilter,
                            Paging = directive.Paging
                        });
                }

            return nextSet;

        }

        public static PopulationDirective Find(this IEnumerable<PopulationDirective> populateRelationships, string name)
        {
            if (populateRelationships != null)
                foreach (var directive in populateRelationships)
                {
                    var parts = directive.Name.Split('.');
                    if (parts[0] == name)
                        return directive;
                }

            return null;
        }
    }
}
