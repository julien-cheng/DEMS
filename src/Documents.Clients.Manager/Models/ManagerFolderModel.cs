namespace Documents.Clients.Manager.Models
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class ManagerFolderModel : ModelBase
    {
        public FolderIdentifier Identifier { get; set; }
        public string Name { get; set; }

        public Dictionary<string, object> Fields { get; set; }
    }
}