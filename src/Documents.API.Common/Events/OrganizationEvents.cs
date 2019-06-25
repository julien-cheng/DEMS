namespace Documents.API.Common.Events
{
    public class OrganizationGetEvent : OrganizationEventBase
    {
        public override bool Audited => false;
    }
    public class OrganizationGetAllEvent : OrganizationEventBase { }
    public class OrganizationPutEvent : OrganizationEventBase { }
    public class OrganizationPostEvent : OrganizationEventBase { }
    public class OrganizationDeleteEvent : OrganizationEventBase { }
}
