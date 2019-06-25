namespace Documents.Queues.Tasks.Synchronize
{
    using System.Threading.Tasks;

    public class Program
    {
        public async static Task Main(string[] args)
        {
            await QueuedApplication.StartAsync(() => new LaunchProfile[]
            {
                new LaunchProfile { ApplicationType=typeof(SynchronizeManagerTask), FixedInstanceCount=2 },
                new LaunchProfile { ApplicationType=typeof(SynchronizeTask) }
            });
        }
    }
}
