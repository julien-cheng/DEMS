namespace Documents.Clients.Manager.Exceptions
{
    public class ArgumentErrorException : ExceptionBase
    {
        public ArgumentErrorException(string argument) 
            : base(System.Net.HttpStatusCode.BadRequest, $"Missing Argument: {argument}")
        {}
    }
}
