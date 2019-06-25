namespace Documents.API.Common.Exceptions
{
    using Documents.API.Common.Models;
    using System;
    using System.Net;

    public class DocumentsException : Exception
    {
        public string ServerStackTrace { get; }
        public HttpStatusCode StatusCode { get; protected set; }

        public DocumentsException(APIResponse wrapper)
            : base(wrapper.ExceptionType == "UnknownException"
                ? $"Server Returned Error: {(int)wrapper.StatusCode} {wrapper.StatusCode} {wrapper.ExceptionType}"
                : wrapper.Exception)
        {
            ServerStackTrace = wrapper.ExceptionStack;
            StatusCode = wrapper.StatusCode;
        }

        public DocumentsException(string message)
            : base(message)
        {
        }
    }
}
