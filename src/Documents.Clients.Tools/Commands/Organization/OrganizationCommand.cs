namespace Documents.Clients.Tools.Commands.Organization
{
    using Documents.API.Common.Models;
    using Documents.Clients.Tools.Commands.Organization.Provision;
    using McMaster.Extensions.CommandLineUtils;
    using System.Linq;
    using System.Threading.Tasks;

    [Subcommand("get", typeof(Get))]
    [Subcommand("delete", typeof(Delete))]
    [Subcommand("provision", typeof(OrganizationProvision))]
    [Subcommand("metadata", typeof(OrganizationMetadataCommand))]
    [Command(Description = "operations on Organizations")]
    class OrganizationCommand : CommandBase
    {
        public void RenderOrganization(OrganizationModel model, bool showPrivileges = false)
        {
            if (model != null)
            {
                Table("Organization", new
                {
                    model.Identifier.OrganizationKey,
                    model.Name
                });

                if (showPrivileges)
                {
                    RenderPrivileges("organization", model.OrganizationPrivileges);
                    RenderPrivileges("folder", model.FolderPrivileges);
                    RenderPrivileges("file", model.FilePrivileges);
                }
            }
            else
                throw new System.Exception("Organization does not exist");
        }

        [Command(Description = "get an organization object")]
        class Get : CommandBase
        {
            [Argument(0, Description = "OrganizationIdentifier")]
            public string OrganizationIdentifier { get; }

            [Option(Description = "show privileges (default: false)")]
            public bool Privileges { get; } = false;

            [Option(Description = "get list of all organizations in lieu of OrganzationKey")]
            public bool All { get; } = false;

            protected async override Task ExecuteAsync()
            {
                if (All)
                {
                    var models = await API.Organization.GetAllAsync();
                    Table("Organizations", models.Rows.Select(o => new
                    {
                        o.Identifier.OrganizationKey,
                        o.Name
                    }));
                }
                else
                {
                    var organizationIdentifier = GetOrganizationIdentifier(OrganizationIdentifier);

                    var model = await API.Organization.GetOrThrowAsync(organizationIdentifier);

                    GetParent<OrganizationCommand>().RenderOrganization(model, Privileges);
                }
            }
        }

        class Delete : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var organizationIdentifier = GetOrganizationIdentifier(Key);

                var model = await API.Organization.DeleteAsync(organizationIdentifier);
            }
        }

    }
}
