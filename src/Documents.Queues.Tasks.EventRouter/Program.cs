namespace Documents.Queues.Tasks.EventRouter
{
    using System.Threading.Tasks;

    public class Program
    {
        public async static Task Main(string[] args)
        {
            await QueuedApplication.StartAsync(QueuedApplication.RunAsync<EventRouterTask>());
        }
    }
}
