namespace Documents.Clients.Tools.Commands
{
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    [Subcommand("token", typeof(Health))]
    class SecurityCommand : CommandBase
    {
        class Health : CommandBase
        {
            protected override Task ExecuteAsync()
            {
                Console.WriteLine(
                    JsonConvert.SerializeObject(new
                    {
                        Root.Connection.Token,
                        Root.Connection.UserIdentifier,
                        Root.Connection.ClientClaims,
                        Root.Connection.UserAccessIdentifiers
                    },
                    Formatting.Indented)
                );

                return Task.FromResult(0);
            }
        }
    }
}
