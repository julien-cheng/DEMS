namespace Documents.Clients.Tools.Commands
{
    using Documents.Queues.Messages;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    class NotifyCommand : CommandBase
    {
        [Required, Argument(0)]
        public string UserIdentifier { get; }

        [Required, Argument(1)]
        public string Template { get; }

        [Argument(2)]
        public string Model { get; }

        protected async override Task ExecuteAsync()
        {
            await API.Queue.EnqueueAsync("Notify", new NotifyMessage
            {
                RecipientIdentifier = GetUserIdentifier(UserIdentifier),
                TemplateName = Template,
                Model = Model != null
                    ? JsonConvert.DeserializeObject(Model)
                    : null
            });
        }
    }
}
