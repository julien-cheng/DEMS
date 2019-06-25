namespace Documents.API.Exceptions
{
    using System.Net;

    public class ObjectDoesNotExistException : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
        public ObjectDoesNotExistException(string message = "Object Does Not Exist") : base(message) { }
    }
}
