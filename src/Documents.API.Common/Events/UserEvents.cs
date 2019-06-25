namespace Documents.API.Common.Events
{
    public class UserGetEvent : UserEventBase { }
    public class UserPutEvent : UserEventBase { }
    public class UserPostEvent : UserEventBase { }
    public class UserDeleteEvent : UserEventBase { }
    public class UserAuthenticated : UserEventBase { }
    public class UserImpersonated : UserEventBase { }
}
