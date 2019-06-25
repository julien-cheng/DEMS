namespace Documents.Store.SqlServer.Stores
{
    using Documents.API.Common;
    using Documents.API.Common.Exceptions;
    using Documents.API.Common.Models;
    using Documents.Store.MetadataFilters;
    using Documents.Store.SqlServer.Entities;
    using Documents.Store.SqlServer.Stores.Models;
    using Documents.Store.Utilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class OrganizationStore : StoreBase<OrganizationModel, OrganizationIdentifier, Organization>, IOrganizationStore
    {
        public OrganizationStore(ISecurityContext securityContext, DocumentsContext database, IServiceProvider serviceProvider) 
            : base(securityContext, database, serviceProvider)
        {
        }

        protected override Task PrivilegeCheckRead(OrganizationModel model)
            => Task.FromResult(model.PrivilegeCheck("read", SecurityContext));

        protected override Task PrivilegeCheckWrite(OrganizationModel model)
            => Task.FromResult(model.PrivilegeCheck("write", SecurityContext));

        protected override Task PrivilegeCheckCreate(OrganizationIdentifier identifier)
        {
            if (!new[] { new ACLModel(":organization:create") }.SatisfiedBy(SecurityContext))
                throw new DocumentsSecurityException("organization", "create");

            return Task.FromResult(true);
        }

        protected override Task PrivilegeCheckDelete(OrganizationModel model)
            => Task.FromResult(model.PrivilegeCheck("delete", SecurityContext));

        protected override string CacheKey(OrganizationIdentifier identifier)
        {
            return $"organization {identifier.OrganizationKey}";
        }

        protected override Expression<Func<Organization, bool>> WhereClause(OrganizationIdentifier identifier)
        {
            return o => o.OrganizationKey == identifier.OrganizationKey;
        }

        public override async Task<PagedResults<OrganizationModel>> LoadRelatedToAsync<TRelatedModel>(
            TRelatedModel related,
            PopulationDirective filters,
            IEnumerable<PopulationDirective> populateRelationships,
            Action<OrganizationModel> securityPrepare
        )
        {
            if (related == null) // GetAll organizations
            {
                var organizations = await DoIncludes(Database.Organization
                    .AsNoTracking())
                    .Where(o => o.OrganizationKey != null) // deleted
                    .Select(o => o.ToModel())
                    .ToListAsync();

                var included = new List<OrganizationModel>();
                foreach (var organization in organizations)
                {
                    securityPrepare?.Invoke(organization);
                    if (filters != null && !filters.MetadataFilter.SatisfiedBy(organization))
                        continue;
                    if (!organization.PrivilegeCheck("read", SecurityContext, throwOnFail: false))
                        continue;

                    await PopulateFoldersAsync(organization, populateRelationships);
                    await PopulateUsersAsync(organization, populateRelationships);

                    included.Add(organization);
                }

                var paging = filters.Paging;
                var page = included.Skip(paging.PageSize * paging.PageIndex).Take(paging.PageSize).ToList();
                // todo: sort

                return new PagedResults<OrganizationModel>
                {
                    Rows = page,
                    TotalMatches = included.Count
                };
            }

            throw new Exception("Unknown relationship type");
        }

        private async Task PopulateFoldersAsync(
            OrganizationModel organizationModel,
            IEnumerable<PopulationDirective> populateRelationships)
        {
            var folderRelationship = populateRelationships.Find(nameof(OrganizationModel.Folders));
            if (folderRelationship != null)
            {
                organizationModel.Folders = await GetStore<IFolderStore>().LoadRelatedToAsync(
                    organizationModel,
                    folderRelationship,
                    populateRelationships.Prune(nameof(OrganizationModel.Folders)),
                    (folder) =>
                    {
                        folder.FolderPrivileges = organizationModel.FolderPrivileges.CopyTo(folder.FolderPrivileges, OrganizationModel.Tier);
                        folder.FolderMetadata = organizationModel.FolderMetadata.CopyTo(folder.FolderMetadata, OrganizationModel.Tier);
                    }
                );
            }
        }

        private async Task PopulateUsersAsync(
            OrganizationModel organizationModel,
            IEnumerable<PopulationDirective> populateRelationships)
        {
            var userRelationship = populateRelationships.Find(nameof(OrganizationModel.Users));
            if (userRelationship != null)
            {
                organizationModel.Users = await GetStore<IUserStore>().LoadRelatedToAsync(
                    organizationModel,
                    userRelationship,
                    populateRelationships.Prune(nameof(OrganizationModel.Users))
                );
            }
        }

        protected async override Task PopulateRelated(OrganizationModel model, Organization entity, IEnumerable<PopulationDirective> populateRelationships)
        {
            await PopulateFoldersAsync(model, populateRelationships);
            await PopulateUsersAsync(model, populateRelationships);
        }

        protected override Task<Organization> ToEntity(OrganizationModel model)
        {
            return Task.FromResult(model.ToEntity());
        }

        protected override Task<OrganizationModel> ToModel(Organization entity, OrganizationIdentifier identifier)
        {
            return Task.FromResult(entity.ToModel());
        }

        protected override string[] IncludedFields()
        {
            return new[] {
                nameof(Organization.Privileges)
            };
        }

        protected override void UpdateEntity(Organization entity, OrganizationModel model)
        {
            entity.Name = model.Name;

            if (model.OrganizationMetadata != null
                || model.FolderMetadata != null
                || model.FileMetadata != null)

                entity.Metadata = JsonConvert.SerializeObject(new Dictionary<string, IDictionary<string, string>>
                {
                    { OrganizationModel.Tier, model.OrganizationMetadata.ExtractTier(OrganizationModel.Tier) },
                    { FolderModel.Tier, model.FolderMetadata.ExtractTier(OrganizationModel.Tier) },
                    { FileModel.Tier, model.FileMetadata.ExtractTier(OrganizationModel.Tier) }
                });

            if (model.OrganizationPrivileges != null
                || model.FolderPrivileges != null
                || model.FilePrivileges != null)
            {
                var privilegeList = new List<Privilege>();
                privilegeList.AddRange(model.OrganizationPrivileges.ExtractTier(OrganizationModel.Tier, OrganizationModel.Tier));
                privilegeList.AddRange(model.FolderPrivileges.ExtractTier(OrganizationModel.Tier, FolderModel.Tier));
                privilegeList.AddRange(model.FilePrivileges.ExtractTier(OrganizationModel.Tier, FileModel.Tier));

                Database.RemoveRange(entity.Privileges);
                entity.Privileges = privilegeList;
            }
            
            base.SetEtag(entity, model, nameof(Organization.UpdateVersion));
        }

        protected override void SoftDelete(Organization entity)
        {
            entity.DeletedKey = entity.OrganizationKey;
            entity.OrganizationKey = null;
        }
    }
}
