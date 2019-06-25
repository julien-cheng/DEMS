namespace Documents.API.Common.Exceptions
{
    using Documents.API.Common.Models;

    public class DocumentsSecurityException : DocumentsException
    {
        public DocumentsSecurityException(APIResponse wrapper)
            : base(wrapper)
        {
        }

        public DocumentsSecurityException(string type, string privilege)
            : base($"Unauthorized: type:{type} privilege:{privilege}")
        {
            this.StatusCode = System.Net.HttpStatusCode.Unauthorized;
        }
    }
}
