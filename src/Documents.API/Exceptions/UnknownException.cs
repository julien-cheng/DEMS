namespace Documents.API.Exceptions
{
    using System.Net;

    public class UnknownException : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;
        public UnknownException() : base("Unknown Exception") { }
    }
}
