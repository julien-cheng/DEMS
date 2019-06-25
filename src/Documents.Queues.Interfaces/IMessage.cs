namespace Documents.Queues.Interfaces
{
    public interface IMessage
    {
        string ID { get; set; }
        string Message { get; set; }
        int Retries { get; set; }
        string Callback { get; set; }
    }
}