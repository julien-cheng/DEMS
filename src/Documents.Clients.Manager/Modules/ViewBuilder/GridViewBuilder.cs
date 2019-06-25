

namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class GridViewBuilder
    {
        public static GridViewModel BuildGridView(int pageIndex, int pageSize, IEnumerable<FileModel> filteredFiles, List<IItemQueryResponse> records, List<GridColumnSpecification> columns, string title)
        {
            return BuildGridView(pageIndex, pageSize, records, filteredFiles.Count(), null,columns, title);
        }

        public static GridViewModel BuildGridView(int pageIndex, int pageSize, List<IItemQueryResponse> items, List<AllowedOperation> operations, List<GridColumnSpecification> columns, string title)
        {
            return BuildGridView(pageIndex, pageSize, items, items.Count, operations, columns, title);
        }

        public static GridViewModel BuildGridView(int pageIndex, int pageSize, List<IItemQueryResponse> items, List<GridColumnSpecification> columns, string title)
        {
            return BuildGridView(pageIndex, pageSize, items, items.Count, null, columns, title);
        }

        public static GridViewModel BuildGridView(int pageIndex, int pageSize, List<IItemQueryResponse> items,int filteredCount, List<AllowedOperation> operations, List<GridColumnSpecification> columns, string title)
        {
            var grid = new GridViewModel
            {
                Rows = new List<IItemQueryResponse>(),
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            grid.TotalRows = items.Count();
            grid.RowsInPage = Math.Min(grid.TotalRows, grid.PageSize);
            grid.PageCount = (grid.TotalRows / grid.PageSize)
                + (
                    (grid.TotalRows % grid.PageSize == 0)
                        ? 0
                        : 1
                );
            grid.IsLastPage = (grid.PageIndex == grid.PageCount - 1);

            // Here we're calculating paging, and sending that back on the grid view.
            grid.Rows = items
                .OrderBy(f => f.Name) // TODO: I Think this sort need to be based off something other than the name /this is not dynamic
                .Skip(grid.PageSize * grid.PageIndex)
                .Take(grid.PageSize);

            grid.GridColumns = columns;

            grid.AllowedOperations = operations;

            grid.Title = title;

            return grid;
        }
    }
}
