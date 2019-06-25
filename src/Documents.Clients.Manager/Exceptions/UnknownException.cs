namespace Documents.Clients.Manager.Exceptions
{
    public class UnknownException : ExceptionBase
    {
        public UnknownException() : base(System.Net.HttpStatusCode.InternalServerError) { }
    }
}
