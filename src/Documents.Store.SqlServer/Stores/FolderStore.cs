namespace Documents.Store.SqlServer.Stores
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Store.Exceptions;
    using Documents.Store.SqlServer.Entities;
    using Documents.Store.Utilities;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class FolderStore : StoreBase<FolderModel, FolderIdentifier, Folder>, IFolderStore
    {
        public FolderStore(
            ISecurityContext securityContext,
            DocumentsContext database,
            IServiceProvider serviceProvider
        ) : base(securityContext, database, serviceProvider)
        {
        }

        public string PrivilegeRead { get; set; } = "read";

        private async Task<bool> OrganizationPrivilegeCheck(OrganizationIdentifier identifier, string privilegeName)
        {
            return (
                    await GetStore<IOrganizationStore>()
                        .GetOneAsync(identifier)
                )
                .PrivilegeCheck(privilegeName, SecurityContext);
        }

        protected override Task PrivilegeCheckRead(FolderModel model)
            => Task.FromResult(model.PrivilegeCheck(PrivilegeRead, SecurityContext));

        protected override Task PrivilegeCheckWrite(FolderModel model)
            => Task.FromResult(model.PrivilegeCheck("write", SecurityContext));

        protected override Task PrivilegeCheckCreate(FolderIdentifier identifier)
            => OrganizationPrivilegeCheck(identifier as OrganizationIdentifier, "folder:create");

        protected override Task PrivilegeCheckDelete(FolderModel model)
            => Task.FromResult(model.PrivilegeCheck("delete", SecurityContext));

        protected override string CacheKey(FolderIdentifier identifier)
        {
            return $"folder {identifier.OrganizationKey}/{identifier.FolderKey}";
        }

        private async Task PopulateFilesAsync(
            FolderModel folderModel,
            IEnumerable<PopulationDirective> populateRelationships,
            OrganizationModel organization = null)
        {
            var fileRelationship = populateRelationships.Find(nameof(FolderModel.Files));
            if (fileRelationship != null)
            {
                if (organization == null)
                    organization = await GetStore<IOrganizationStore>().GetOneAsync(folderModel.Identifier);

                folderModel.Files = await GetStore<IFileStore>().LoadRelatedToAsync(
                    folderModel,
                    fileRelationship,
                    populateRelationships.Prune(nameof(FolderModel.Files)),
                    (file) =>
                    {
                        file.FilePrivileges = organization.FilePrivileges.CopyTo(file.FilePrivileges, OrganizationModel.Tier);
                        file.FileMetadata = organization.FileMetadata.CopyTo(file.FileMetadata, OrganizationModel.Tier);

                        file.FilePrivileges = folderModel.FilePrivileges.CopyTo(file.FilePrivileges, FolderModel.Tier);
                        file.FileMetadata = folderModel.FileMetadata.CopyTo(file.FileMetadata, FolderModel.Tier);
                    }
                );
            }
        }

        public override async Task<PagedResults<FolderModel>> LoadRelatedToAsync<TRelatedModel>(
            TRelatedModel related, 
            PopulationDirective filters, 
            IEnumerable<PopulationDirective> populateRelationships, 
            Action<FolderModel> securityPrepare
        )
        {
            if (typeof(TRelatedModel) == typeof(OrganizationModel))
            {
                var organization = related as OrganizationModel;

                // Load the folders from the database
                var folders = DoIncludes(Database.Folder
                    .AsNoTracking())
                    .Where(f => f.Organization.OrganizationKey == organization.Identifier.OrganizationKey)
                    .Where(f => f.FolderKey != null) // deleted
                    .Select(f => f.ToModel(organization.Identifier.OrganizationKey));

                var paging = filters.Paging;
                var included = new List<FolderModel>();

                int skip = paging.PageIndex * paging.PageSize;

                foreach (var folder in folders)
                {
                    securityPrepare(folder);
                    if (!folder.PrivilegeCheck("read", SecurityContext, throwOnFail: false))
                        continue;

                    if (skip > 0)
                        skip--;
                    else
                    {
                        await PopulateFilesAsync(folder, populateRelationships, organization);
                        included.Add(folder);
                    }

                    if (included.Count >= paging.PageSize)
                        break;
                }

                //var page = included.Skip(paging.PageSize * paging.PageIndex).Take(paging.PageSize).ToList();
                // todo: sort

                return new PagedResults<FolderModel>
                {
                    Rows = included,
                    TotalMatches = included.Count
                };
            }

            throw new Exception("Unknown relationship type");
        }

        public async override Task ModelInheritance(FolderModel model, Folder entity)
        {
            var organization = await (GetStore<IOrganizationStore>() as OrganizationStore)
                .GetOneAsync(model.Identifier as OrganizationIdentifier);
            
            model.FolderMetadata = organization.FolderMetadata.CopyTo(model.FolderMetadata, OrganizationModel.Tier);
            model.FolderPrivileges = organization.FolderPrivileges.CopyTo(model.FolderPrivileges, OrganizationModel.Tier);
            
        }
        
        protected async override Task PopulateRelated(FolderModel model, Folder entity, IEnumerable<PopulationDirective> populateRelationships)
        {
            await PopulateFilesAsync(model, populateRelationships);
        }

        // duplicated in User
        private async Task<long> OrganizationIDLookup(OrganizationIdentifier identifier)
        {
            var organizationEntity = await Database.Organization
                .Where(o => o.OrganizationKey == identifier.OrganizationKey)
                .FirstOrDefaultAsync();

            if (organizationEntity == null)
                throw new StoreException("Organization does not exist");
            else
                return organizationEntity.OrganizationID;
        }

        protected async override Task<Folder> ToEntity(FolderModel model)
        {
            return model.ToEntity(await OrganizationIDLookup(model.Identifier));
        }

        protected override Task<FolderModel> ToModel(Folder entity, FolderIdentifier identifier)
        {
            return Task.FromResult(entity.ToModel(identifier.OrganizationKey));
        }

        protected override Expression<Func<Folder, bool>> WhereClause(FolderIdentifier identifier)
        {
            return (f => f.FolderKey == identifier.FolderKey
                && f.Organization.OrganizationKey == identifier.OrganizationKey);
        }
        protected override void UpdateEntity(Folder entity, FolderModel model)
        {
            if (model.FolderMetadata != null
                || model.FileMetadata != null)

                entity.Metadata = JsonConvert.SerializeObject(new Dictionary<string, IDictionary<string, string>>
                {
                    { FolderModel.Tier, model.FolderMetadata.ExtractTier(FolderModel.Tier) },
                    { FileModel.Tier, model.FileMetadata.ExtractTier(FolderModel.Tier) }
                });

            if (model.FolderPrivileges != null
                || model.FilePrivileges != null)
            {
                var privilegeList = new List<Privilege>();
                privilegeList.AddRange(model.FolderPrivileges.ExtractTier(FolderModel.Tier, FolderModel.Tier));
                privilegeList.AddRange(model.FilePrivileges.ExtractTier(FolderModel.Tier, FileModel.Tier));

                Database.RemoveRange(entity.Privileges);
                entity.Privileges = privilegeList;
            }

            base.SetEtag(entity, model, nameof(Folder.UpdateVersion));
        }

        protected override string[] IncludedFields()
        {
            return new[]
            {
                "Privileges"
            };
        }

        protected override void SoftDelete(Folder entity)
        {
            entity.DeletedKey = entity.FolderKey;
            entity.FolderKey = null;
        }
    }
}
