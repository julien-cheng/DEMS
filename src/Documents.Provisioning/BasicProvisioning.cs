namespace Documents.Provisioning
{
    using Documents.API.Client;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.API.Common.Security;
    using Documents.Provisioning.Models;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    public class BasicProvisioning
    {
        private const int AES_KEY_SIZE_BYTES = 32;
        private readonly Connection api;

        private string idSystem;
        private string idOrganizationMember;

        public BasicProvisioning(Connection api)
        {
            this.api = api;
        }

        public async Task<OrganizationModel> OrganizationApply(BasicOrganizationModel configurationModel)
        {
            EnsureValidConfigurationBasic(configurationModel);

            // create the organization
            var organizationIdentifier = CreateOrganizationIdentifier(configurationModel);
            var organizationModel = await api.Organization.GetAsync(organizationIdentifier);

            if (organizationModel == null)
            {
                organizationModel = new OrganizationModel(organizationIdentifier)
                    {
                        Name = configurationModel.Name
                    }
                    .InitializeEmptyMetadata()
                    .InitializeEmptyPrivileges();
            }

            organizationModel.Write("type", "basic");

            // define user access identifiers
            idSystem = "u:system";
            idOrganizationMember = $"o:{organizationIdentifier.OrganizationKey}";

            organizationModel.WriteACLs("read", idSystem, idOrganizationMember);
            organizationModel.WriteACLs("write", idSystem);
            organizationModel.WriteACLs("delete", idSystem);

            organizationModel.WriteACLs("folder:create", idSystem, idOrganizationMember);

            organizationModel.WriteACLs("user:create", idSystem);
            organizationModel.WriteACLs("user:read", idSystem, idOrganizationMember);
            organizationModel.WriteACLs("user:write", idSystem);
            organizationModel.WriteACLs("user:delete", idSystem);
            organizationModel.WriteACLs("user:credentials", idSystem);
            organizationModel.WriteACLs("user:identifiers", idSystem);
            organizationModel.WriteACLs("user:impersonate", idSystem);

            organizationModel.WriteACLsForFolder("create", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFolder("read", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFolder("write", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFolder("delete", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFolder("file:create", idSystem, idOrganizationMember);

            organizationModel.WriteACLsForFile("read", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFile("write", idSystem, idOrganizationMember);
            organizationModel.WriteACLsForFile("delete", idSystem, idOrganizationMember);

            await api.Organization.PutAsync(organizationModel);

            await this.ConfigureBackendAsync(organizationModel, configurationModel);

            return organizationModel;
        }

        private async Task ConfigureBackendAsync(OrganizationModel organizationModel, BasicOrganizationModel configurationModel)
        {
            // create a private folder to store backend configuration
            var privateFolder = new FolderModel(new FolderIdentifier(organizationModel.Identifier, ":private"))
                .InitializeEmptyMetadata()
                .InitializeEmptyPrivileges();

            // write the backend configuration into the folder's metadata
            var backendConfiguration = new BackendConfiguration
            {
                DriverTypeName = "Documents.Backends.Drivers.FileSystem.Driver, Documents.Backends.Drivers.FileSystem",
                ConfigurationJSON = JsonConvert.SerializeObject(new
                {
                    configurationModel.BasePath
                })
            };
            privateFolder.Write(MetadataKeyConstants.BACKEND_CONFIGURATION, backendConfiguration);
            privateFolder.WriteACLs("read", idSystem);
            privateFolder.WriteACLs("write", idSystem);
            privateFolder.WriteACLs("gateway", idSystem, idOrganizationMember);

            await api.Folder.PutAsync(privateFolder);
        }

        private OrganizationIdentifier CreateOrganizationIdentifier(BasicOrganizationModel model)
        {
            return new OrganizationIdentifier(model.OrganizationKey);
        }

        private void EnsureValidConfigurationBasic(BasicOrganizationModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
        }
    }
}
