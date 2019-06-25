namespace Documents.API.Common.Models
{
    public class QueueMessage
    {
        public string ID { get; set; }
        public string Message { get; set; }
        public int Retries { get; set; }
        public string Callback { get; set; }
    } 
}