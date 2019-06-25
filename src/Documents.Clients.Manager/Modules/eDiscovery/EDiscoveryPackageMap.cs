using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Modules.eDiscovery
{
    public class EDiscoveryPackageMap
    {
        public Dictionary<string, PackageAttributes> Map { get; set; } = new Dictionary<string, PackageAttributes>();
    }

    public class PackageAttributes
    {
        public string CustomName { get; set; }
    }
}
