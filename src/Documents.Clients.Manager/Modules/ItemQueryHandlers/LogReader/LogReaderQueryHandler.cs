namespace Documents.Clients.Manager.Modules.ItemQueryHandlers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.LogReader;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class LogReaderQueryHandler : BaseItemQueryHandler
    {
        private readonly ManagerConfiguration ManagerConfiguration;
        private readonly LogReaderService LogReaderService;

        public LogReaderQueryHandler(
            PathService pathService, 
            APIConnection connection, 
            ManagerConfiguration managerConfiguration, 
            FileService fileService,
            LogReaderService logReaderService
        ) : base(pathService, connection, managerConfiguration, fileService)
        {
            this.ManagerConfiguration = managerConfiguration;
            this.LogReaderService = logReaderService;
        }

        protected override List<AllowedOperation> GetPageAllowedOperations(PathIdentifier identifier)
        {
            var ops = new List<AllowedOperation>();

            if (ManagerConfiguration.IsFeatureEnabledSearch)
                ops.Add(AllowedOperation.GetAllowedOperationSearch(identifier));

            return ops;
        }

        protected override void FilterFiles(PathIdentifier identifier)
        {
            // We shouldn't be returning a list of any files in the ediscovery folder.
            filteredFiles = new List<FileModel>();
        }

        private static List<AllowedOperation> GetAllowedOperationsForRecipient(FolderIdentifier folderIdentifier, ExternalUser recipient)
        {
            return new List<AllowedOperation>() {
                AllowedOperation.GetAllowedOperationRegenPassword(folderIdentifier, recipient.Email),
                AllowedOperation.GetAllowedOperationRemoveRecipient(folderIdentifier, recipient.Email),
            };
        }

        protected async override Task BuildViews(List<IModule> activeModules, int pageIndex, int pageSize, FolderModel folder, PathIdentifier identifier, string userTimeZone)
        {
            this.page.Views = new List<Models.Responses.IViewModel>();

            /*var instructions = new DataViewModel
            {
                DataModel = null,
                DataSchema = new ManagerFieldObject()
                {
                    IsCollapsed = false,
                    Properties = new Dictionary<string, ManagerFieldBaseSchema>() {
                            { "Instructions", new ManagerFieldNull() {
                                Description = @"<p>
                                                  Some stuff here maybe?
                                                </p>",
                                IsReadOnly = true,
                                Order = 0,
                                Title = "Log Reader Info"
                            }
                        },
                    }
                },
                AllowedOperations = null
            };

            this.page.Views.Add(instructions);*/

            var logs = await LogReaderService.ReadLog(identifier);

            var grid = new GridViewModel
            {
                Rows = new List<IItemQueryResponse>(),
                PageIndex = pageIndex,
                PageSize = pageSize,
                Title = "Audit Log",
                GridColumns = new List<GridColumnSpecification>() {
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "generated", Label = "Generated" },
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "action", Label = "Action" },
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "initiator", Label = "User" },
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "description", Label = "Description" },
                },
            };

            grid.TotalRows = logs.Rows.Count();
            grid.RowsInPage = Math.Min(grid.TotalRows, grid.PageSize);
            grid.PageCount = (grid.TotalRows / grid.PageSize)
                + (
                    (grid.TotalRows % grid.PageSize == 0)
                        ? 0
                        : 1
                );
            grid.IsLastPage = (grid.PageIndex == grid.PageCount - 1);

            // Here we're calculating paging, and sending that back on the grid view.
            grid.Rows = logs.Rows
                .OrderByDescending(f => f.Generated)
                .Skip(grid.PageSize * grid.PageIndex)
                .Take(grid.PageSize)
                .Select(l => new LogEntryModel
                {
                    Name = "Name",
                    Key = "Key",
                    Action = l.ActionType,
                    Generated = l.Generated.ConvertToLocal(userTimeZone),
                    Initiator = l.InitiatorUserIdentifier?.UserKey,
                    Description = l.Description
                });

            this.page.Views.Add(grid);
        }
    }
}
