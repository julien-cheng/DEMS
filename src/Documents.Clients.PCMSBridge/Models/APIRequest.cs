namespace Documents.Clients.PCMSBridge.Models
{
    public class APIRequest
    {
        public AttachmentContext Context { get; set; }
        public string APIKey { get; set; }
    }

    public class APIRequest<T> : APIRequest
    {
        public T Parameters { get; set; }
    }
}