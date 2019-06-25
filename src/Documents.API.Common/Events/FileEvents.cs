namespace Documents.API.Common.Events
{
    public class FileGetEvent : FileEventBase
    {
        public override bool Audited => false; 
    }
    public class FilePutEvent : FileEventBase { }
    public class FilePostEvent : FileEventBase { }
    public class FileDeleteEvent : FileEventBase { }

    public class FileContentsUploadCompleteEvent : FileEventBase { }
    public class FileDownloadEvent : FileEventBase { }

    public class FileTextContentChangeEvent: FileEventBase
    {
        public string Type { get; set; }
    }
}
