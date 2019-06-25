namespace Documents.Queues.Tasks.LogReader
{
    using System.Threading.Tasks;

    public class Program
    {
        public async static Task Main(string[] args)
        {
            await QueuedApplication.StartAsync(QueuedApplication.RunAsync<LogReaderTask>());
        }
    }
}
