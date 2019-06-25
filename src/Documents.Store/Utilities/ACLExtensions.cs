namespace Documents.Store.Utilities
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Store.Exceptions;
    using System.Collections.Generic;
    using System.Linq;

    public static class ACLExtensions
    {
        public static bool SatisfiedBy(this IEnumerable<ACLModel> acls, IEnumerable<string> userIdentifiers)
        {
            var success = false;

            if (acls == null || !acls.Any())
                success = false;
            else
                success = (acls.All(a => a.RequiredIdentifiers.Intersect(userIdentifiers).Any()));

            return success;
        }

        public static bool SatisfiedBy(this IEnumerable<ACLModel> acls, ISecurityContext securityContext)
        {
            return acls.SatisfiedBy(securityContext.SecurityIdentifiers);
        }
    }
}