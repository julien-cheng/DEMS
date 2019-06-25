namespace Documents.Queues.Tasks
{
    using Microsoft.Extensions.Logging;
    using PeterKottas.DotNetCore.WindowsService.Interfaces;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class MicroService: IMicroService
    {
        protected CancellationTokenSource CancelExecution { get; set; }
        private Task ServiceExecuting = null;
        private readonly ILogger Logger;
        private readonly Func<CancellationToken, Task> Entry;

        public MicroService(ILogger logger, Func<CancellationToken, Task> entry)
        {
            Logger = logger;
            Entry = entry;
        }

        void IMicroService.Start()
        {
            CancelExecution = new CancellationTokenSource();

            Logger.LogInformation("Starting via Windows Service");
            ServiceExecuting = Task.Run(async () =>
            {
                try
                {
                    await Entry(CancelExecution.Token);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failure in task processing: {0}", e);
                    throw;
                }
                Logger.LogInformation("Service is stopping");

            });
            Logger.LogInformation("Started");
        }

        void IMicroService.Stop()
        {
            Logger.LogInformation("Cancelling execution");
            CancelExecution.Cancel();
            try
            {
                Logger.LogInformation("Waiting for service to stop");
                ServiceExecuting.Wait(5000);
            }
            catch (Exception) { }
            Logger.LogInformation("Service Stopped");
        }
    }
}
