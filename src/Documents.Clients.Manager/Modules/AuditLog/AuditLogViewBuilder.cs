namespace Documents.Clients.Manager.Modules.AuditLog
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.AuditLog;
    using Documents.Clients.Manager.Models.Responses;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class AuditLogViewBuilder
    {
        public static GridViewModel BuildPagedGridView(int pageIndex, int pageSize, List<AuditLogEntry> entries, FolderIdentifier folderIdentifier, Func<IItemQueryResponse, object> sortSelector = null)
        {
            // first we need to convert recipeients into IItemQueryResponse objects
            var queryResponseEntries = new List<ManagerAuditLogEntryModel>();

            foreach (var entry in entries)
            {
                queryResponseEntries.Add(new ManagerAuditLogEntryModel()
                {
                    Name= String.Format($"{entry.Message} {entry.Created}"),
                    Key = String.Empty,
                    Message = entry.Message,
                    UserName = entry.UserName,
                    EntryType = AuditLogEntry.GetTitleForEntryType(entry.EntryType),
                    // Probably do some parsing for this date format
                    Created = entry.Created,
                    AllowedOperations = GetAllowedOperationsForAuditLogEntry(entry, folderIdentifier).ToArray(),
                });
            }

            var grid = new GridViewModel
            {
                Rows = new List<IItemQueryResponse>(),
                Title = GridViewModel.GRID_TITLES_EDISOVERY_AUDIT_LOG,
                PageIndex = pageIndex,
                PageSize = pageSize,
                GridColumns = new List<GridColumnSpecification>() {
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "message", Label= "Entry"  },
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "userName", Label= "User Name"  },
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "entryType", Label= "Entry Type"  },
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "created", Label= "Created"  },
                },
            };

            grid.TotalRows = entries.Count();
            grid.RowsInPage = Math.Min(grid.TotalRows, grid.PageSize);
            grid.PageCount = (grid.TotalRows / grid.PageSize)
                + (
                    (grid.TotalRows % grid.PageSize == 0)
                        ? 0
                        : 1
                );
            grid.IsLastPage = (grid.PageIndex == grid.PageCount - 1);

            // Here we're calculating paging, and sending that back on the grid view.
            grid.Rows = queryResponseEntries
                .OrderByDescending(f => (sortSelector == null)
                    ? f.Created
                    : sortSelector(f)
                )
                .Skip(grid.PageSize * grid.PageIndex)
                .Take(grid.PageSize);

            grid.AllowedOperations = new List<AllowedOperation>() { };

            return grid;
        }

        private static List<AllowedOperation> GetAllowedOperationsForAuditLogEntry(AuditLogEntry entry, FolderIdentifier folderIdentifier)
        {
            return new List<AllowedOperation>()
            {
            };
        }
    }
}