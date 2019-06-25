namespace Documents.Queues.Tasks.Voicebase
{
    using System.Threading.Tasks;

    public class Program
    {
        public async static Task Main(string[] args)
        {
            await QueuedApplication.StartAsync(
                QueuedApplication.RunAsync<VoicebaseTask>(),
                QueuedApplication.RunAsync<VoicebaseCallbackTask>()
            );
        }
    }
}
