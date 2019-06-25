namespace Documents.API.Common.Models
{
    using Documents.API.Common.Exceptions;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class PrivilegeModelBase
    {
        public abstract IEnumerable<ACLModel> Privilege(string key);

        public bool PrivilegeCheck(string key, ISecurityContext securityContext, bool throwOnFail = true)
            => PrivilegeCheck(key, securityContext.SecurityIdentifiers, throwOnFail);
        
        public bool PrivilegeCheck(string key, IEnumerable<string> userIdentifiers, bool throwOnFail = true)
        {
            return PrivilegeCheck(Privilege(key), key, userIdentifiers, throwOnFail);
        }

        public bool PrivilegeCheck(IEnumerable<ACLModel> acls, string key, IEnumerable<string> userIdentifiers, bool throwOnFail = true)
        {
            bool success =
                acls != null
                && acls.Any()
                && acls.All(a => a.RequiredIdentifiers.Intersect(userIdentifiers).Any());

            if (!success && throwOnFail)
                throw new DocumentsSecurityException(this.GetType().Name.Replace("Model", ""), key);

            return success;
        }
    }
}
