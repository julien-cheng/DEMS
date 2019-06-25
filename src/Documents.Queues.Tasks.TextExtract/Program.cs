﻿namespace Documents.Queues.Tasks.TextExtract
{
    using System.Threading.Tasks;

    public class Program
    {
        public async static Task Main(string[] args)
        {
            await QueuedApplication.StartAsync(QueuedApplication.RunAsync<TextExtractTask>());
        }
    }
}
