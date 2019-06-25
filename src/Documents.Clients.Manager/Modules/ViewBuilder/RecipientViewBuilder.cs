namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.eDiscovery;
    using Documents.Clients.Manager.Models.Responses;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class RecipientViewBuilder
    {
        public static GridViewModel BuildPagedGridView(
            int pageIndex,
            int pageSize,
            List<ExternalUser> recipients,
            FolderIdentifier folderIdentifier,
            string userTimeZone,
            string GridTitle,
            List<AllowedOperation> operations,
            Func<FolderIdentifier, ExternalUser, List<AllowedOperation>> getRecipientAllowedOperations)
        {
            // first we need to convert recipeients into IItemQueryResponse objects
            var queryResponseRecipients = new List<IItemQueryResponse>();

            foreach (var recipient in recipients)
            {
                queryResponseRecipients.Add(new ManagerRecipientModel()
                {
                    FirstName = recipient.FirstName,
                    LasttName = recipient.LastName,
                    Email = recipient.Email,
                    //PasswordHash = recipient.PasswordHash,
                    MagicLink = recipient.MagicLink,
                    ExpirationDate = recipient.ExpirationDate.ConvertToLocal(userTimeZone),
                    AllowedOperations = getRecipientAllowedOperations(folderIdentifier, recipient).ToArray(),
                });
            }

            var grid = new GridViewModel
            {
                Rows = new List<IItemQueryResponse>(),
                PageIndex = pageIndex,
                PageSize = pageSize,
                Title = GridTitle,
                GridColumns = new List<GridColumnSpecification>() {
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "name", Label= "Name"  },
                    new GridColumnSpecification(){ IsSortable = true, KeyName = "email", Label= "Email"  },
                    new GridColumnSpecification(){ IsSortable = false, KeyName = "magicLink", Label= "Magic Link"  },
                    new GridColumnSpecification(){ IsSortable = false, KeyName = "expirationDate", Label= "Expires On"  },
                    new GridColumnSpecification(){ IsSortable = false, KeyName = "allowedOperations", Label = "Actions" }
                },                
            };

            grid.TotalRows = recipients.Count();
            grid.RowsInPage = Math.Min(grid.TotalRows, grid.PageSize);
            grid.PageCount = (grid.TotalRows / grid.PageSize)
                + (
                    (grid.TotalRows % grid.PageSize == 0)
                        ? 0
                        : 1
                );
            grid.IsLastPage = (grid.PageIndex == grid.PageCount - 1);

            // Here we're calculating paging, and sending that back on the grid view.
            grid.Rows = queryResponseRecipients
                .OrderBy(f => f.Name) // TODO: I Think this sort need to be based off something other than the name /this is not dynamic
                .Skip(grid.PageSize * grid.PageIndex)
                .Take(grid.PageSize);

            grid.AllowedOperations = operations;


            return grid;
        }
    }
}
