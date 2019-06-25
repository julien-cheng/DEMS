namespace Documents.Clients.Manager.Models.ViewSets
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class BaseSet
    {
        public FileIdentifier RootFileIdentifier { get; set; }
        public IEnumerable<AllowedOperation> AllowedOperations { get; set; }
        public IEnumerable<ManagerFileView> Views { get; set; }
        public MessageDetails Message { get; set; }

        public class MessageDetails
        {
            public string Type { get; set; } = "alert"; // alert, dialog, toastr
            public string Title { get; set; }
            public string Body { get; set; }
        }
    }
}