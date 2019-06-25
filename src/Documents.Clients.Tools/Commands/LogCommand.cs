namespace Documents.Clients.Tools.Commands
{
    using Documents.API.Common.Models;
    using McMaster.Extensions.CommandLineUtils;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class LogCommand: CommandBase
    {
        [Argument(0, Description = "Key")]
        public string Key { get; }

        [Option("--action-type")]
        public string ActionType { get; }

        [Option("--organization-key")]
        public string OrganizationKey { get; }

        [Option("--folder-key")]
        public string FolderKey { get; }

        [Option("--file-key")]
        public string FileKey { get; }

        [Option("--user-key")]
        public string UserKey { get; }

        [Option("--before")]
        public string Before { get; }

        [Option("--after")]
        public string After { get; }

        protected async override Task ExecuteAsync()
        {
            var folderIdentifier = GetFolderIdentifier(Key);
            var filters = new List<MetadataMatchModel>();

            if (OrganizationKey != null)
            {
                filters.Add(new MetadataMatchModel
                {
                    Name = "OrganizationKey",
                    Operator = "==",
                    Value = OrganizationKey
                });
            }
            if (FolderKey != null)
            {
                if (OrganizationKey == null)
                    throw new System.Exception("Must specify --organization-key if using --folder-key");

                filters.Add(new MetadataMatchModel
                {
                    Name = "FolderKey",
                    Operator = "==",
                    Value = FolderKey
                });
            }
            if (FileKey != null)
            {
                if (FolderKey == null || OrganizationKey == null)
                    throw new System.Exception("Must specify --folder-key and --organization-key if using --file-key");

                filters.Add(new MetadataMatchModel
                {
                    Name = "FileKey",
                    Operator = "==",
                    Value = FileKey
                });
            }
            if (UserKey != null)
            {
                if (OrganizationKey == null)
                    throw new System.Exception("Must specify --organization-key if using --user-key");

                filters.Add(new MetadataMatchModel
                {
                    Name = "UserKey",
                    Operator = "==",
                    Value = UserKey
                });
            }

            if (Before != null)
                filters.Add(new MetadataMatchModel
                {
                    Name = "Generated",
                    Operator = "<",
                    Value = Before
                });

            if (After != null)
                filters.Add(new MetadataMatchModel
                {
                    Name = "Generated",
                    Operator = ">=",
                    Value = After
                });

            if (ActionType != null)
                filters.Add(new MetadataMatchModel
                {
                    Name = "ActionType",
                    Operator = "==",
                    Value = ActionType
                });

            var paged = await API.Log.LoadAsync(new[] {
                    new PopulationDirective(null)
                    {
                        MetadataFilter = filters
                    }
                });

            Table("Audit Log", paged.Rows.Select(a => new
            {
                a.Generated,
                a.ActionType,
                a.Description,
                a.FileIdentifier,
                a.UserAgent,
                a.InitiatorUserIdentifier
            }));
        }
    }
}
