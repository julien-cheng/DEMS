namespace Documents.Clients.Manager.Exceptions
{
    public class FileNotFoundException : ExceptionBase
    {
        public FileNotFoundException() : base(System.Net.HttpStatusCode.NotFound) { }
    }
}
