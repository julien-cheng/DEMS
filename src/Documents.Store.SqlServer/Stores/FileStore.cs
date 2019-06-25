namespace Documents.Store.SqlServer.Stores
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Store.Exceptions;
    using Documents.Store.MetadataFilters;
    using Documents.Store.SqlServer.Entities;
    using Documents.Store.Utilities;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class FileStore : StoreBase<FileModel, FileIdentifier, File>, IFileStore
    {
        public FileStore(ISecurityContext securityContext, DocumentsContext database, IServiceProvider serviceProvider)
            : base(securityContext, database, serviceProvider)
        {
        }

        private async Task<bool> FolderPrivilegeCheck(FolderIdentifier identifier, string privilegeName)
        {
            return (
                    await GetStore<IFolderStore>()
                        .GetOneAsync(identifier)
                )
                .PrivilegeCheck(privilegeName, SecurityContext);
        }

        protected override Task PrivilegeCheckRead(FileModel model)
            => Task.FromResult(model.PrivilegeCheck("read", SecurityContext));

        protected override Task PrivilegeCheckWrite(FileModel model)
            => Task.FromResult(model.PrivilegeCheck("write", SecurityContext));

        protected override Task PrivilegeCheckCreate(FileIdentifier identifier)
            => FolderPrivilegeCheck(identifier as FolderIdentifier, "file:create");

        protected override Task PrivilegeCheckDelete(FileModel model)
            => Task.FromResult(model.PrivilegeCheck("delete", SecurityContext));

        protected override string CacheKey(FileIdentifier identifier)
        {
            return $"file {identifier.OrganizationKey}/{identifier.FolderKey}/{identifier.FileKey}";
        }

        public async override Task ModelInheritance(FileModel model, File entity)
        {
            var organization = await (GetStore<IOrganizationStore>() as OrganizationStore)
                .GetOneAsync(model.Identifier as OrganizationIdentifier);
            var folder = await (GetStore<IFolderStore>() as FolderStore)
                .GetOneAsync(model.Identifier as FolderIdentifier);

            model.FileMetadata = organization.FileMetadata.CopyTo(model.FileMetadata, OrganizationModel.Tier);
            model.FilePrivileges = organization.FilePrivileges.CopyTo(model.FilePrivileges, OrganizationModel.Tier);

            model.FileMetadata = folder.FileMetadata.CopyTo(model.FileMetadata, FolderModel.Tier);
            model.FilePrivileges = folder.FilePrivileges.CopyTo(model.FilePrivileges, FolderModel.Tier);
        }

        public override async Task<PagedResults<FileModel>> LoadRelatedToAsync<TRelatedModel>(TRelatedModel related, PopulationDirective filters, IEnumerable<PopulationDirective> populateRelationships, Action<FileModel> securityPrepare)
        {
            if (typeof(TRelatedModel) == typeof(FolderModel))
            {
                var folder = related as FolderModel;

                // Load the files from the database
                var query = Database.File
                    .Where(f =>
                        f.Folder.Organization.OrganizationKey == folder.Identifier.OrganizationKey
                        && f.Folder.FolderKey == folder.Identifier.FolderKey
                    )
                    .Where(f => f.Status == FileStatus.Normal)
                    .Where(f => f.FileKey != null) // deleted
                    .Include(f => f.Privileges)
                    .AsNoTracking();

                var fileEntities = await query.ToListAsync();

                var files = fileEntities.Select(e => e.ToModel(folder.Identifier.OrganizationKey, folder.Identifier.FolderKey));

                var included = new List<FileModel>();
                foreach (var file in files)
                {
                    securityPrepare(file);
                    if (!filters.MetadataFilter.SatisfiedBy(file))
                        continue;
                    if (!file.PrivilegeCheck("read", SecurityContext, throwOnFail: false))
                        continue;

                    included.Add(file);
                }

                var paging = filters.Paging;
                var page = included.Skip(paging.PageSize * paging.PageIndex).Take(paging.PageSize).ToList();
                // todo: sort

                return new PagedResults<FileModel>
                {
                    Rows = page,
                    TotalMatches = included.Count
                };
            }

            throw new InvalidRelationshipException();
        }

        private async Task<long> FolderIDLookup(FolderIdentifier identifier)
        {
            var folderEntity = await Database.Folder
                .Where(f => f.FolderKey == identifier.FolderKey)
                .Where(f => f.Organization.OrganizationKey == identifier.OrganizationKey)
                .FirstOrDefaultAsync();

            if (folderEntity == null)
                throw new ObjectNotFound();
            else
                return folderEntity.FolderID;
        }

        protected async override Task<File> ToEntity(FileModel model)
        {
            return model.ToEntity(await FolderIDLookup(model.Identifier));
        }

        protected override Task<FileModel> ToModel(File entity, FileIdentifier identifier)
        {
            return Task.FromResult(entity.ToModel(identifier.OrganizationKey, identifier.FolderKey));
        }

        protected override void UpdateEntity(File entity, FileModel model)
        {
            if (model.FileMetadata != null)
                entity.Metadata = JsonConvert.SerializeObject(new Dictionary<string, IDictionary<string, string>>
                {
                    { FileModel.Tier, model.FileMetadata.ExtractTier(FileModel.Tier) }
                });

            if (model.FilePrivileges != null)
            {
                var privilegeList = new List<Privilege>();
                privilegeList.AddRange(model.FilePrivileges.ExtractTier(FileModel.Tier, FileModel.Tier));

                Database.RemoveRange(entity.Privileges);
                entity.Privileges = privilegeList;
            }

            entity.Name = model.Name;
            entity.Created = model.Created;
            entity.Modified = model.Modified;
            entity.MimeType = model.MimeType;
            entity.Length = model.Length;

            base.SetEtag(entity, model, nameof(File.UpdateVersion));
        }

        protected override Expression<Func<File, bool>> WhereClause(FileIdentifier identifier)
        {
            return (f => f.FileKey == identifier.FileKey
                && f.Folder.FolderKey == identifier.FolderKey
                && f.Folder.Organization.OrganizationKey == identifier.OrganizationKey);
        }

        protected override string[] IncludedFields()
        {
            return new[]
            {
                "Privileges"
            };
        }

        public async Task<string> FileLocatorGetAsync(FileIdentifier identifier)
        {
            return await WithOneAsync(identifier, pair => pair.Entity.FileLocator);
        }

        public async Task FileLocatorSetAsync(FileIdentifier identifier, string fileLocator)
        {
            await UpdateOneAsync(identifier, pair => {
                pair.Entity.FileLocator = fileLocator;
                return 0;
            });
        }

        public async Task UploadingStatusSetAsync(FileIdentifier identifier, bool isUploading)
        {
            await UpdateOneAsync(identifier, pair =>
            {
                pair.Entity.Status = isUploading 
                    ? FileStatus.Uploading
                    : FileStatus.Normal;

                return true;
            });
        }

        public async Task HashSetAsync(FileIdentifier identifier, string md5, string sha1, string sha256)
        {
            await UpdateOneAsync(identifier, pair =>
            {
                pair.Entity.MD5 = md5;
                pair.Entity.SHA1 = sha1;
                pair.Entity.SHA256 = sha256;
                
                return true;
            });
        }


        protected override void SoftDelete(File entity)
        {
            entity.DeletedKey = entity.FileKey;
            entity.FileKey = null;
        }

        public async Task Move(FileIdentifier destination, FileIdentifier source)
        {
            await PrivilegeCheckCreate(destination);

            var folderStore = GetStore<IFolderStore>();
            var folderID = await FolderIDLookup(destination as FolderIdentifier);

            await UpdateOneAsync(source, pair =>
            {
                pair.Entity.FolderID = folderID;
                pair.Entity.FileKey = destination.FileKey;

                return true;
            });
        }
    }
}
