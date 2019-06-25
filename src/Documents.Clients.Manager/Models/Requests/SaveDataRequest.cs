namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class SaveDataRequest
    {
        // I want this to work for either file or folder, if I use FileIdentifierModel, then filekey is always Required. 
        // That's not what I want. 
        public FileIdentifier FileIdentifier { get; set; }
        public FolderIdentifier FolderIdentifier { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}