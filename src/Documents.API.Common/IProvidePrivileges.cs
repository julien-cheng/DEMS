namespace Documents.API.Common
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public interface IProvidePrivileges
    {
        IEnumerable<ACLModel> Privilege(string key);
        IDictionary<string, IEnumerable<ACLModel>> PrivilegesFlattened { get; }
    }
}
