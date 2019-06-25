namespace Documents.Clients.Tools.Commands
{
    using McMaster.Extensions.CommandLineUtils;
    using System;
    using System.Threading.Tasks;

    [Subcommand("health", typeof(Health))]
    class ServerCommand : CommandBase
    {
        class Health : CommandBase
        {
            protected async override Task ExecuteAsync()
            {
                var started = DateTime.Now;

                var healthy = await API.HealthGetAsync();

                Table("Health", new
                {
                    Server = API.BaseURL.ToString(),
                    ms = DateTime.Now.Subtract(started).TotalMilliseconds
                });
            }
        }
    }
}
