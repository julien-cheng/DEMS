namespace Documents.API.Common.Exceptions
{
    using Documents.API.Common.Models;

    public class DocumentsRetryException : DocumentsException
    {
        public DocumentsRetryException(APIResponse wrapper)
            : base(wrapper)
        {
        }
    }
}
