namespace Documents.API.Exceptions
{
    using System;
    using System.Net;

    public abstract class ExceptionBase : Exception
    {
        public abstract HttpStatusCode StatusCode { get; }

        public ExceptionBase(string message)
            : base(message)
        {
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