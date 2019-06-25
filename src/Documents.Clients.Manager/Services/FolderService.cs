namespace Documents.Clients.Manager.Services
{
    using Common;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Modules;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class FolderService : ServiceBase<ManagerFolderModel, FolderIdentifier>
    {
        public FolderService(APIConnection connection) : base(connection)
        {

        }

        public async override Task<IEnumerable<ManagerFolderModel>> QueryAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.QueryAllAsync(Connection.UserIdentifier as OrganizationIdentifier, cancellationToken);
        }

        public async Task<IEnumerable<ManagerFolderModel>> QueryAllAsync(OrganizationIdentifier organizationIdentifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            // todo: should be push an organization identifier into here to make these canonical?
            var organization = await Connection.Organization.GetAsync(organizationIdentifier,
                new List<PopulationDirective>
                {
                    { new PopulationDirective(nameof(OrganizationModel.Folders)) }
                },
                cancellationToken: cancellationToken);

            return organization.Folders.Rows.Select(ModelConvert).ToList();
        }

        public async Task<ManagerFolderModel> SaveFormData(SaveDataRequest saveDataRequest)
        {
            // First we're going to 'open' the file, and get all that metadata filled out.
            var folderModel = await Connection.Folder.GetAsync(saveDataRequest.FolderIdentifier);

            SchemaForm.UpdateFormData(saveDataRequest.Data, folderModel);

            await Connection.Folder.PutAsync(folderModel);

            return ModelConvert(folderModel);
        }

        public ManagerFolderModel ModelConvert(FolderModel folderModel)
        {
            var managerModel = new ManagerFolderModel
            {
                Identifier = folderModel.Identifier
            };

            managerModel.Fields = new Dictionary<string, object>();

            var fields = ReadMetadataFields(folderModel);
            if (fields != null)
                foreach (var field in fields)
                    managerModel.Fields.Add(
                        field.Identifier,
                        folderModel.Read<string>(field.Key)
                    );

            return managerModel;
        }

        public async override Task<ManagerFolderModel> QueryOneAsync(FolderIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ModelConvert(
                await Connection.Folder.GetAsync(identifier, cancellationToken: cancellationToken)
            );
        }

        public async override Task<ManagerFolderModel> UpsertOneAsync(ManagerFolderModel model, CancellationToken cancellationToken = default(CancellationToken))
        {

            var folderModel = new FolderModel(model.Identifier);

            folderModel.InitializeEmptyMetadata();

            foreach (var field in model.Fields)
            {
                folderModel.Write(field.Key, field.Value);
            }

            folderModel = await Connection.Folder.PutAsync(folderModel, cancellationToken: cancellationToken);

            return ModelConvert(folderModel);
        }

        public async override Task<FolderIdentifier> DeleteOneAsync(FolderIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Connection.Folder.DeleteAsync(identifier, cancellationToken);
            return identifier;
        }

        private List<ManagerFieldsMetadataModel> ReadMetadataFields(FolderModel folderModel)
        {
            return folderModel.Read<List<ManagerFieldsMetadataModel>>(ManagerFieldsMetadataModel.METADATA_KEY_LIST);
        }

    }
}