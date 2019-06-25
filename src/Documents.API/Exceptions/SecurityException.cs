namespace Documents.API.Exceptions
{
    using System.Net;

    public class SecurityException : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
        public SecurityException(string message = "Security exception") : base(message) { }
    }

    public class SecurityExceptionImpersonationDenied : SecurityException
    {
        public SecurityExceptionImpersonationDenied() : base("Impersonation denied") { }
    }
    public class SecurityExceptionImpersonationInvalidTarget : SecurityException
    {
        public SecurityExceptionImpersonationInvalidTarget() : base("Invalid impersonation target") { }
    }
}
