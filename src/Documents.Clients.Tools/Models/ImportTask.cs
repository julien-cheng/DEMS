namespace Documents.Clients.Tools.Models
{
    using System.Collections.Generic;

    public class ImportTask
    {
        public string OrganizationKey { get; set; }
        public string FolderKey { get; set; }
        public string PathKey { get; set; }
        public string Filename { get; set; }

        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public TaskTypes TaskType { get; set; }

        public enum TaskTypes
        {
            Import,
            Delete,
            DeleteFolders,
            SimulateUpload
        }
    }
}
