namespace Documents.Queues.Interfaces
{
    public class SimpleMessage : IMessage
    {
        public string ID { get; set; }
        public string Message { get; set; }
        public int Retries { get; set; }
        public string Callback { get; set; }
    }
}
