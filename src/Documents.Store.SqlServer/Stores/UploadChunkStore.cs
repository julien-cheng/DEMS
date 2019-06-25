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

    public class UploadChunkStore : StoreBase<UploadChunkModel, UploadChunkIdentifier, UploadChunk>, IUploadChunkStore
    {
        public UploadChunkStore(ISecurityContext securityContext, DocumentsContext database, IServiceProvider serviceProvider)
            : base(securityContext, database, serviceProvider)
        {
        }

        public override async Task<PagedResults<UploadChunkModel>> LoadRelatedToAsync<TRelatedModel>(TRelatedModel related, PopulationDirective filters, IEnumerable<PopulationDirective> populateRelationships, Action<UploadChunkModel> securityPrepare)
        {
            if (typeof(TRelatedModel) == typeof(UploadModel))
            {
                var upload = related as UploadModel;

                // Load the files from the database
                var uploadChunks = await DoIncludes(Database.UploadChunk
                    .AsNoTracking())
                    .Where(u =>
                        u.Upload.File.Folder.Organization.OrganizationKey == upload.Identifier.OrganizationKey
                        && u.Upload.File.Folder.FolderKey == upload.Identifier.FolderKey
                        && u.Upload.File.FileKey == upload.Identifier.FileKey
                        && u.Upload.UploadKey == upload.Identifier.UploadKey
                    )
                    .Select(f => f.ToModel())
                    .ToListAsync();

                var included = new List<UploadChunkModel>();

                foreach (var uploadChunk in uploadChunks)
                {
                    securityPrepare?.Invoke(uploadChunk);

                    included.Add(uploadChunk);
                }

                var paging = filters.Paging;
                var page = included.Skip(paging.PageSize * paging.PageIndex).Take(paging.PageSize).ToList();
                // todo: sort

                return new PagedResults<UploadChunkModel>
                {
                    Rows = page,
                    TotalMatches = included.Count
                };
            }

            throw new InvalidRelationshipException();
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

        private async Task<long> UploadIDLookup(UploadIdentifier identifier)
        {
            var uploadEntity = await Database.Upload
                .Where(uc => uc.File.Folder.Organization.OrganizationKey == identifier.OrganizationKey)
                .Where(uc => uc.File.Folder.FolderKey == identifier.FolderKey)
                .Where(uc => uc.File.FileKey == identifier.FileKey)
                .Where(uc => uc.UploadKey == identifier.UploadKey)
                .FirstOrDefaultAsync();

            if (uploadEntity == null)
                throw new StoreException("Upload does not exist");
            else
                return uploadEntity.UploadID;
        }

        protected async override Task<UploadChunk> ToEntity(UploadChunkModel model)
        {
            return model.ToEntity(
                await UploadIDLookup(model.Identifier)
            );
        }

        protected override Task<UploadChunkModel> ToModel(UploadChunk entity, UploadChunkIdentifier identifier)
        {
            return Task.FromResult(entity.ToModel());
        }

        protected override void UpdateEntity(UploadChunk entity, UploadChunkModel model)
        {
            entity.State = model.State;
            entity.Success = model.Success;
        }

        protected override Expression<Func<UploadChunk, bool>> WhereClause(UploadChunkIdentifier identifier)
        {
            return (uc => uc.ChunkKey == identifier.UploadChunkKey
                && uc.Upload.UploadKey == identifier.UploadKey
                && uc.Upload.File.FileKey == identifier.FileKey
                && uc.Upload.File.Folder.FolderKey == identifier.FolderKey
                && uc.Upload.File.Folder.Organization.OrganizationKey == identifier.OrganizationKey);
        }

        protected override string[] IncludedFields()
        {
            return new[]
            {
                $"{nameof(UploadChunk.Upload)}",
                $"{nameof(UploadChunk.Upload)}.{nameof(Upload.File)}",
                $"{nameof(UploadChunk.Upload)}.{nameof(Upload.File)}.{nameof(File.Folder)}",
                $"{nameof(UploadChunk.Upload)}.{nameof(Upload.File)}.{nameof(File.Folder)}.{nameof(Folder.Organization)}",
            };
        }
    }
}
