namespace Documents.API.Exceptions
{
    using System.Net;

    public class InvalidIdentifierException : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

        public InvalidIdentifierException() : base("Identifier is not valid")
        {
        }
    }
}
