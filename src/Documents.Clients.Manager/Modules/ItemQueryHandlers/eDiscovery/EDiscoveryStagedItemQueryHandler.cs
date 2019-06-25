

namespace Documents.Clients.Manager.Modules.ItemQueryHandlers
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Modules.eDiscovery;
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class EDiscoveryStagedItemQueryHandler : EDiscoveryDatedPackageItemQueryHandler
    {
        private readonly ManagerConfiguration ManagerConfiguration;

        public EDiscoveryStagedItemQueryHandler(PathService pathService, APIConnection connection,
            ManagerConfiguration managerConfiguration, FileService fileService) : base(pathService, connection, managerConfiguration, fileService)
        {
            this.ManagerConfiguration = managerConfiguration;
        }

        protected override List<AllowedOperation> GetPageAllowedOperations(PathIdentifier identifier)
        {
            var ops = new List<AllowedOperation>();

            if (ManagerConfiguration.IsFeatureEnabledSearch)
                ops.Add(AllowedOperation.GetAllowedOperationSearch(identifier));

            var recipients = state.Folder.MetaEDiscoveryRecipientListRead();
            if(state.Folder.Files.Rows.Any(f => EDiscoveryUtility.GetCurrentShareState(f) == EDiscoveryShareState.Staged))
            {
                ops.Add(AllowedOperation.GetAllowedOperationPublish(identifier as FolderIdentifier, recipients.Count));
            }

            return ops;
        }

        protected override Task BuildViews(List<IModule> activeModules, int pageIndex, int pageSize, FolderModel folder, PathIdentifier identifier,  string userTimeZone)
        {
            base.BuildViews(activeModules, pageIndex, pageSize, folder, identifier, userTimeZone);

            // Now we need to insert a view at the top which will be the staged item instructions
            var instructions = new DataViewModel
            {
                DataModel = null,
                DataSchema = new ManagerFieldObject()
                {
                    IsCollapsed = false,
                    Properties = new Dictionary<string, ManagerFieldBaseSchema>() {
                            { "Instructions", new ManagerFieldNull() {
                                Description = @"<p>
                                                    The files here have not been ""Turned Over"" yet. This is a staging area to help you prepare Discovery Packages. 
                                                    Once the package is ready, click ""Turn Over"" to share these files with the case recipients.
                                                </p>
                                                <p>
                                                    <b>This system does not automatically generate emails when you click turn over. </b>
                                                    Instead, it is recommended that you email the recipients every time new Discovery packages are created 
                                                    to let them know you've provided additional Discovery.
                                                </p>
                                                ",
                                IsReadOnly = true,
                                Order = 0,
                                Title = "Not Shared Yet Instructions"
                            }
                        },
                    }
                },
                AllowedOperations = null
            };

            this.page.Views.Insert(0,instructions);

            return Task.FromResult(0);
        }

        protected override void InternalFilterFiles(PathIdentifier identifier)
        {
            filteredFiles = filteredFiles
                .Where(f => f.Read<string>(MetadataKeyConstants.E_DISCOVERY_SHARE_STATE_META_KEY) == EDiscoveryShareState.Staged.ToString())
                .Where(f => !f.Read<bool>(MetadataKeyConstants.HIDDEN));
        }
    }
}
