namespace Documents.Clients.Tools.Commands.Tools
{
    using McMaster.Extensions.CommandLineUtils;

    [Subcommand("video", typeof(VideoToolsCommand))]
    class ToolsCommand : CommandBase
    {
    }
}
