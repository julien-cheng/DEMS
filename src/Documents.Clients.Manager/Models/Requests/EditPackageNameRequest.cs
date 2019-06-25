using Documents.API.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Models.Requests
{
    public class EditPackageNameRequest: ModelBase
    {
        public FolderIdentifier FolderIdentifier { get; set; }
        public string PackageName { get; set; }
        public string CustomName { get; set; }
    }
}
