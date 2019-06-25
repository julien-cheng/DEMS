namespace Documents.Clients.Tools.Commands.Organization.Provision
{
    using Documents.Provisioning;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    class Basic : CommandBase
    {
        [Required]
        [Option("--name <NAME>", Description = "Organization Name")]
        public string Name { get; }

        [Required]
        [Option("--organizationKey <KEY>", Description = "Organization Key")]
        public string OrganizationKey { get; }

        [Required]
        [Option("--basepath <BASEPATH>", Description = "Storage location on server's disk")]
        public string BasePath { get; } = null;

        protected async override Task ExecuteAsync()
        {
            var provisioning = new BasicProvisioning(API);

            var basic = new Provisioning.Models.BasicOrganizationModel
            {
                Name = Name,
                OrganizationKey = OrganizationKey,
                BasePath = BasePath
            };

            var model = await provisioning.OrganizationApply(basic);

            GetParent<OrganizationCommand>().RenderOrganization(model);
        }
    }
}
