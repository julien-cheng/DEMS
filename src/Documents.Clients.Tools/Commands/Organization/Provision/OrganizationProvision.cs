namespace Documents.Clients.Tools.Commands.Organization.Provision
{
    using McMaster.Extensions.CommandLineUtils;

    [Subcommand("pcms", typeof(PCMS))]
    [Subcommand("basic", typeof(Basic))]
    [Command(Description = "create, initialize, update organizations")]
    class OrganizationProvision : CommandBase
    {
    }
}
