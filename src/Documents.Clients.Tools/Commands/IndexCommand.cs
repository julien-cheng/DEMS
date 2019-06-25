namespace Documents.Clients.Tools.Commands
{
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using McMaster.Extensions.CommandLineUtils;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    [Subcommand("file", typeof(File))]
    [Subcommand("folder", typeof(Folder))]
    [Subcommand("organization", typeof(Organization))]
    [Subcommand("nuke", typeof(Nuke))]
    class IndexCommand : CommandBase
    {
        class File : CommandBase
        {
            [Option]
            public bool Delete { get; } = false;

            [Required, Argument(0)]
            public string FileIdentifier { get; }

            protected async override Task ExecuteAsync()
            {
                await API.Queue.EnqueueAsync("Index", new IndexMessage
                {
                    Action = Delete
                        ? IndexMessage.IndexActions.DeleteFile
                        : IndexMessage.IndexActions.DeleteFolder,
                    Identifier = GetFileIdentifier(FileIdentifier)
                });
            }
        }

        class Folder : CommandBase
        {
            [Option]
            public bool Delete { get; } = false;

            [Required, Argument(0)]
            public string FolderIdentifier { get; }

            protected async override Task ExecuteAsync()
            {
                await API.Queue.EnqueueAsync("Index", new IndexMessage
                {
                    Action = Delete
                        ? IndexMessage.IndexActions.DeleteFolder
                        : IndexMessage.IndexActions.IndexFolder,
                    Identifier = new FileIdentifier(GetFolderIdentifier(FolderIdentifier))
                });
            }
        }

        class Organization : CommandBase
        {
            [Option]
            public bool Delete { get; } = false;

            [Required, Argument(0)]
            public string OrganizationIdentifier { get; }

            protected async override Task ExecuteAsync()
            {
                await API.Queue.EnqueueAsync("Index", new IndexMessage
                {
                    Action = Delete 
                        ? IndexMessage.IndexActions.DeleteOrganization
                        : IndexMessage.IndexActions.IndexOrganization,
                    Identifier = new FileIdentifier(
                        GetOrganizationIdentifier(OrganizationIdentifier).OrganizationKey,
                        string.Empty,
                        null)
                });
            }
        }

        class Nuke : CommandBase
        {
            [Option]
            public bool ImSure { get; } = false;

            protected async override Task ExecuteAsync()
            {
                if (ImSure)
                    await API.Queue.EnqueueAsync("Index", new IndexMessage
                    {
                        Action = IndexMessage.IndexActions.DeleteEntireIndex,
                        Identifier = null
                    });
                else
                    throw new System.Exception("must set --im-sure");
            }
        }
    }
}
