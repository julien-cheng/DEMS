namespace Documents.API.Common.Exceptions
{
    using Documents.API.Common.Models;

    public class DocumentsNotFoundException : DocumentsException
    {
        public DocumentsNotFoundException(APIResponse wrapper)
            : base(wrapper)
        {
        }
    }
}
