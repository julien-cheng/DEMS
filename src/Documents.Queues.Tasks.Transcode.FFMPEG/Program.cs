namespace Documents.Queues.Tasks.Transcode.FFMPEG
{
    using System.Threading.Tasks;

    public class Program
    {
        public async static Task Main(string[] args)
        {
            await QueuedApplication.StartAsync(
                QueuedApplication.RunAsync<TranscodeTask>(),
                QueuedApplication.RunAsync<VideoToolsTask>()
            );
        }
    }
}
