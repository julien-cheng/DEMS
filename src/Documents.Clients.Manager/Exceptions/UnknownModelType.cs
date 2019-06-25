namespace Documents.Clients.Manager.Exceptions
{
    public class UnknownModelType : ExceptionBase
    {
        public UnknownModelType() : base(System.Net.HttpStatusCode.BadRequest ) { }
    }
}
