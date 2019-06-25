namespace Documents.API.Common.Events
{
    public class FolderGetEvent : FolderEventBase
    {
        public override bool Audited => false;
    }
    public class FolderPutEvent : FolderEventBase { }
    public class FolderPostEvent : FolderEventBase { }
    public class FolderDeleteEvent : FolderEventBase { }
}
