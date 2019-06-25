namespace Documents.Store.SqlServer.Stores
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Store.Exceptions;
    using Documents.Store.SqlServer.Entities;
    using Documents.Store.Utilities;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class UploadStore : StoreBase<UploadModel, UploadIdentifier, Upload>, IUploadStore
    {
        public UploadStore(ISecurityContext securityContext, DocumentsContext database, IServiceProvider serviceProvider)
            : base(securityContext, database, serviceProvider)
        {
        }

        private async Task<bool> OrganizationPrivilegeCheck(OrganizationIdentifier identifier, string privilegeName)
        {
            return (
                    await GetStore<OrganizationStore>()
                        .GetOneAsync(identifier)
                )
                .Privilege(privilegeName)
                .SatisfiedBy(SecurityContext);
        }

        private async Task<long> FileIDLookup(FileIdentifier identifier)
        {
            var fileEntity = await Database.File
                .Where(f => f.Folder.Organization.OrganizationKey == identifier.OrganizationKey)
                .Where(f => f.Folder.FolderKey == identifier.FolderKey)
                .Where(f => f.FileKey == identifier.FileKey)
                .FirstOrDefaultAsync();

            if (fileEntity == null)
                throw new StoreException("File does not exist");
            else
                return fileEntity.FileID;
        }

        private async Task<long> UserIDLookup(UserIdentifier identifier)
        {
            var userEntity = await Database.User
                .Where(u => u.Organization.OrganizationKey == identifier.OrganizationKey
                    && u.UserKey == identifier.UserKey)
                .FirstOrDefaultAsync();

            if (userEntity == null)
                throw new StoreException("User does not exist");
            else
                return userEntity.UserID;
        }

        protected async override Task<Upload> ToEntity(UploadModel model)
        {
            return model.ToEntity(
                await FileIDLookup(model.Identifier),
                await UserIDLookup(SecurityContext.UserIdentifier)
            );
        }

        protected override Task<UploadModel> ToModel(Upload entity, UploadIdentifier identifier)
        {
            return Task.FromResult(entity.ToModel());
        }

        protected override void UpdateEntity(Upload entity, UploadModel model)
        {
            if (model.Length != 0)
                entity.Length = model.Length;
        }

        protected override Expression<Func<Upload, bool>> WhereClause(UploadIdentifier identifier)
        {
            return (u => u.UploadKey == identifier.UploadKey
                && u.File.FileKey == identifier.FileKey
                && u.File.Folder.FolderKey == identifier.FolderKey
                && u.File.Folder.Organization.OrganizationKey == identifier.OrganizationKey);
        }

        protected async override Task PopulateRelated(UploadModel model, Upload entity, IEnumerable<PopulationDirective> populateRelationships)
        {
            await PopulateUploadChunks(model, populateRelationships);
        }

        private async Task PopulateUploadChunks(
            UploadModel uploadModel,
            IEnumerable<PopulationDirective> populateRelationships)
        {
            var uploadRelationship = populateRelationships.Find(nameof(UploadModel.Chunks));
            if (uploadRelationship != null)
            {
                uploadModel.Chunks = await GetStore<IUploadChunkStore>().LoadRelatedToAsync(
                    uploadModel,
                    uploadRelationship,
                    populateRelationships.Prune(nameof(FolderModel.Files))
                );
            }
        }

        protected override string[] IncludedFields()
        {
            return new[]
            {
                $"{nameof(Upload.File)}.{nameof(File.Folder)}.{nameof(Folder.Organization)}",
                $"{nameof(Upload.UploadChunks)}",
            };
        }

        public async Task Cleanup(UploadIdentifier identifier)
        {
            await UpdateOneAsync(identifier, (pair) =>
            {
                foreach (var chunk in pair.Entity.UploadChunks)
                    Database.State(chunk, EntityState.Deleted);

                return 0;
            });
        }
    }
}
