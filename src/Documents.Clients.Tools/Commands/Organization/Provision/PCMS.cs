namespace Documents.Clients.Tools.Commands.Organization.Provision
{
    using Documents.Provisioning;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    [Command(Description = "provision an organization for PCMS integration")]
    class PCMS : CommandBase
    {
        [Required]
        [Option("--countyid <ID>", Description = "County ID")]
        public int CountyID { get; }

        [Option("--name <NAME>", Description = "County Name")]
        public string Name { get; }

        [Option("--access-key-id <KEY>", Description = "AWS AccessKeyID")]
        public string AWSAccessKeyID { get; }

        [Option("--secret-access-key <KEY>", Description = "AWS SecretAccessKey")]
        public string AWSSecretAccessKey { get; }

        [Option("--bucket-name <NAME>", Description = "S3 Bucket Name")]
        public string AWSS3BucketName { get; }

        [Option("--ediscovery", Description = "EDiscovery Active")]
        public (bool HasValue, bool IsActive) EDiscoveryActive { get; }

        [Option("--leo", Description = "LEO Active")]
        public (bool HasValue, bool IsActive) LEOActive { get; }

        [Option("--bridge-password <PASSWORD>", Description = "PCMS Bridge Account Password")]
        public string BridgePassword { get; } = null;

        [Option("--use-organization-key <ORGKEY>", Description = "Use the specified organization key instead of generating it from the CountyID")]
        public string UseOrganizationKey { get; } = null;

        [Option(Description = "for extant organizations, backend configuration will not be overwritten by default. if you need to change the s3 info, set this: but BEWARE of the encryption key loss.")]
        public bool OverrideBackendConfiguration { get; } = false;

        [Option("--upgrade-users", Description = "perform user-by-user inspection for updates")]
        public bool UpgradeUsers { get; } = false;

        [Option("--upgrade-folders", Description = "perform folder-by-folder inspection for updates")]
        public bool UpgradeFolders { get; } = false;

        protected async override Task ExecuteAsync()
        {
            var provisioning = new PCMSProvisioning(API);

            var pcms = new Provisioning.Models.PCMSOrganizationModel
            {
                CountyID = CountyID,
                CountyName = Name,
                UseOrganizationKey = UseOrganizationKey,

                AWSAccessKeyID = AWSAccessKeyID,
                AWSSecretAccessKey = AWSSecretAccessKey,
                AWSS3BucketName = AWSS3BucketName,

                EDiscoveryActive = EDiscoveryActive.HasValue ? EDiscoveryActive.IsActive : null as bool?,

                LEOActive = LEOActive.HasValue ? LEOActive.IsActive : null as bool?,

                OverrideBackendConfiguration = OverrideBackendConfiguration
            };

            if (!await provisioning.OrganizationExists(pcms) || OverrideBackendConfiguration)
            {
                // these are only set the first time. for now the provisioner
                // only inserts backend config and a pcms user. So this really
                // only has any effect once... so the guard is actually kinda for show
                pcms.MasterEncryptionKey = provisioning.GenerateMasterEncryptionKey();
            }
            pcms.PCMSBridgeUserPassword = BridgePassword;

            var model = await provisioning.OrganizationApply(pcms);

            if (UpgradeFolders)
                await provisioning.UpgradeFoldersAsync(model.Identifier);

            if (UpgradeUsers)
                await provisioning.UpgradeUsersAsync(model.Identifier);

            GetParent<OrganizationCommand>().RenderOrganization(model);
        }
    }
}
