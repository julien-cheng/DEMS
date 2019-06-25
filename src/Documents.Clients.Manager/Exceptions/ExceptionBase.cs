namespace Documents.Clients.Manager.Exceptions
{
    using System;
    using System.Net;

    public abstract class ExceptionBase : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public ExceptionBase(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
        public ExceptionBase(HttpStatusCode statusCode, string message)
            :base(message)
        {
            StatusCode = statusCode;
        }

        public string Type
        {
            get
            {
                return this.GetType().Name;
            }
        }
    }
}
