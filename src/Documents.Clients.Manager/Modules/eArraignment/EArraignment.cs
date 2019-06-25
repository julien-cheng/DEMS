namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.eArraignment;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class EArraignment : BaseModule, IModule
    {
        public static readonly string E_ARRAIGNMENT_ACTIVE = "eArraignment[isActive]";
        public static readonly string E_ARRAIGNMENT_STATE_CLASS = "blue";

        protected readonly APIConnection connection;
        protected readonly ManagerConfiguration managerConfiguration;

        public EArraignment(APIConnection connection, ManagerConfiguration managerConfiguration)
        {
            this.connection = connection;
            this.managerConfiguration = managerConfiguration;
        }

        public override bool IsModuleActive(FolderModel folder)
        {
            //First we need to determine based on metadata whether eArraignment is active for this folder.
            return folder.Read<bool>(E_ARRAIGNMENT_ACTIVE);
        }

        public override ModuleType ModuleType()
        {
            return Modules.ModuleType.eArraignment;
        }

        public override void SetInitialAllowedOperations(FileModel fileModel, List<AllowedOperation> allowed, PathIdentifier virtualPathIdentifier)
        {
            if (fileModel.Name.ToLower().EndsWith(".pdf") || fileModel.Name.ToLower().EndsWith(".docx"))
            {
                if (fileModel.Name.ToLower().EndsWith(".docx"))
                {
                    var avs = fileModel.Read(MetadataKeyConstants.ALTERNATIVE_VIEWS, defaultValue: new List<AlternativeView>());
                    if (avs.Any(a => a.MimeType == "application/pdf"))
                    {
                        allowed.Add(AllowedOperation.GetAllowedOperationSendToEArraignment(fileModel.Identifier));
                    }
                }
                else
                    allowed.Add(AllowedOperation.GetAllowedOperationSendToEArraignment(fileModel.Identifier));
            }
        }

        public async Task SendToEArraignment(FileIdentifier fileIdentifier)
        {
            await connection.Queue.EnqueueAsync("PCMSRCDAEArraignmentCreation", fileIdentifier);
        }

        public static string GetEArrainmentStateIcon(FileModel fileModel)
        {
            var views = fileModel.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);
            if (views != null && views.Any(v => v.MimeType == "application/xml"))
                return E_ARRAIGNMENT_STATE_CLASS;
            else
                return null;
        }

        public override bool HasHandlerForBatchOperation(ModelBase operation)
        {
            if (operation is SendToEArraignmentRequest)
            {
                return true;
            }
            return false;
        }

        public override async Task<ModelBase> HandleBatchOperation(ModelBase operation)
        {
            if (operation is SendToEArraignmentRequest)
            {
                var sendToEArraignment = operation as SendToEArraignmentRequest;
                await SendToEArraignment(sendToEArraignment.FileIdentifier);
                return null;
            }
            
            return null;
        }
    }
}
