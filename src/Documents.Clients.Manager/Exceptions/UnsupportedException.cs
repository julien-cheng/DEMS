namespace Documents.Clients.Manager.Exceptions
{
    public class UnsupportedException : ExceptionBase
    {
        public UnsupportedException() : base(System.Net.HttpStatusCode.BadRequest ) { }
    }
}
