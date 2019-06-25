namespace Documents.Clients.Manager.Exceptions
{
    public class MalformedKeyException : ExceptionBase
    {
        public MalformedKeyException() : base(System.Net.HttpStatusCode.BadRequest ) { }
    }
}
