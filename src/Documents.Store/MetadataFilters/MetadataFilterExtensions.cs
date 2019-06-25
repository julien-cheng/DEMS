namespace Documents.Store.MetadataFilters
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public static class MetadataFilterExtensions
    {
        public static bool SatisfiedBy(this List<MetadataMatchModel> metadataFilters, IProvideMetadata model)
        {
            if (metadataFilters != null)
                foreach (var filter in metadataFilters)
                {
                    var value = model.Metadata(filter.Name);

                    switch (filter.Operator.ToLower())
                    {
                        case "exact":
                            if (value != filter.Value)
                                return false;
                            break;
                        case "caseinsensitive":
                            if (value?.ToLower() != filter.Value?.ToLower())
                                return false;
                            break;

                    }
                }

            return true;
        }
    }
}