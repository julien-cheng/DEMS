namespace Documents.Clients.Tools.Commands
{
    using Documents.Queues.Messages;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    class SyncCommand : CommandBase
    {
        [Argument(0)]
        public string OrganizationIdentifier { get; }

        [Option]
        public bool Complete { get; }

        [Option]
        public bool Wait { get; }

        [Option]
        public bool SyncAll { get; }

        [Option]
        public int Timeout { get; } = 30000;

        [Option]
        public int Every { get; } = 0;

        protected async override Task ExecuteAsync()
        {
            var syncMessage = (SyncAll)
                ? new SynchronizeMessage
                {
                    Component = "ScheduleEntry"
                }
                : new SynchronizeMessage
                {
                    OrganizationIdentifier = GetOrganizationIdentifier(OrganizationIdentifier),
                    Component = Complete
                        ? "Complete"
                        : "Delta"
                };

            var done = false;
            while(!done)
            {
                await SendMessage(syncMessage);

                if (Every > 0)
                    await Task.Delay(Every);
                else
                    done = true;
            }
        }

        protected async Task SendMessage(SynchronizeMessage syncMessage)
        {
            if (Wait)
            {
                var message = await Root.Connection.Queue.EnqueueWithCallbackWait("SynchronizeManager", syncMessage, Timeout);
                syncMessage = JsonConvert.DeserializeObject<SynchronizeMessage>(message.Message);
                Table("Sync Result", syncMessage);
            }
            else
                await Root.Connection.Queue.EnqueueAsync("SynchronizeManager", syncMessage);

        }
    }
}
