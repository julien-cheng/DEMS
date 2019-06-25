namespace Documents.API.Exceptions
{
    using System.Net;

    public class ConfigurationException : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;

        public ConfigurationException(string message) : base("Configuration Error: " + message)
        {
        }
    }
}
