namespace Documents.API.Common.Exceptions
{
    using Documents.API.Common.Models;
    using System.Net;

    public class ConcurrencyException : DocumentsException
    {
        public ConcurrencyException(APIResponse wrapper)
            : base(wrapper)
        {
            StatusCode = HttpStatusCode.PreconditionFailed;
        }
    }
}
