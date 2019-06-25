namespace Documents.Queues.Interfaces
{
    using System.Threading.Tasks;

    public interface IMessageQueueContext
    {
        Task Initialize();
        Task Shutdown();
    }
}