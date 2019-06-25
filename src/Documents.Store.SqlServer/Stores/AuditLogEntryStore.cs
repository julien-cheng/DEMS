namespace Documents.Store.SqlServer.Stores
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Store.Exceptions;
    using Documents.Store.SqlServer.Entities;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class AuditLogEntryStore : StoreBase<AuditLogEntryModel, AuditLogEntryIdentifier, AuditLogEntry>, IAuditLogEntryStore
    {
        public AuditLogEntryStore(ISecurityContext securityContext, DocumentsContext database, IServiceProvider serviceProvider)
            : base(securityContext, database, serviceProvider)
        {
        }

        private DateTime? TryParseDate(string text)
        {
            if (DateTime.TryParse(text, out DateTime date))
                return date;
            else
                return null;
        }

        public override async Task<PagedResults<AuditLogEntryModel>> LoadRelatedToAsync<TRelatedModel>(
            TRelatedModel related, 
            PopulationDirective filters, 
            IEnumerable<PopulationDirective> populateRelationships, 
            Action<AuditLogEntryModel> securityPrepare
        )
        {
            string organizationKey = null;
            string folderKey = null;
            string userKey = null;
            string fileKey = null;

            if (typeof(TRelatedModel) == typeof(OrganizationIdentifier))
                organizationKey = (related as OrganizationIdentifier)?.OrganizationKey;
            if (typeof(TRelatedModel) == typeof(FolderIdentifier))
                folderKey= (related as FolderIdentifier)?.FolderKey;
            if (typeof(TRelatedModel) == typeof(FileIdentifier))
                fileKey = (related as FileIdentifier)?.FileKey;
            if (typeof(TRelatedModel) == typeof(UserIdentifier))
                fileKey = (related as UserIdentifier)?.UserKey;

            organizationKey = organizationKey ?? filters.MetadataFilter?.Where(m => m.Name == "OrganizationKey").FirstOrDefault()?.Value;
            folderKey = folderKey ?? filters.MetadataFilter?.Where(m => m.Name == "FolderKey").FirstOrDefault()?.Value;
            fileKey = fileKey ?? filters.MetadataFilter?.Where(m => m.Name == "FileKey").FirstOrDefault()?.Value;
            userKey = userKey ?? filters.MetadataFilter?.Where(m => m.Name == "UserKey").FirstOrDefault()?.Value;

            // this is not ideal. it's filtering for a very narrow use case
            bool internalOnly = filters.MetadataFilter?.Where(m => m.Name == "InternalOnly").Any() ?? false;

            var actionTypeFilter = filters.MetadataFilter?.Where(m => m.Name == "ActionType").FirstOrDefault();

            DateTime? generatedAfter = TryParseDate(filters.MetadataFilter?.Where(p => p.Name == "Generated" && p.Operator == ">=").FirstOrDefault()?.Value);
            DateTime? generatedBefore = TryParseDate(filters.MetadataFilter?.Where(p => p.Name == "Generated" && p.Operator == "<").FirstOrDefault()?.Value);

            var query = Database.AuditLog
                .Where(a =>
                    true
                    && (organizationKey == null || a.OrganizationKey == organizationKey)
                    && (folderKey == null || a.FolderKey == folderKey)
                    && (fileKey == null || a.FileKey == fileKey)
                    && (userKey == null || a.UserKey == userKey)
                    && (actionTypeFilter == null || a.ActionType == actionTypeFilter.Value)

                    && (generatedAfter == null || a.Generated >= generatedAfter)
                    && (generatedBefore == null || a.Generated < generatedBefore)

                    && (!internalOnly ||
                        (
                            !a.InitiatorUserKey.StartsWith("leo:")
                            && !a.InitiatorUserKey.StartsWith("Defendant:")
                        )
                    )
                )
                .AsNoTracking()
                .OrderByDescending(a => a.Generated);

            var logEntities = await query.ToListAsync();

            var logs = logEntities.Select(e => e.ToModel());

            var included = new List<AuditLogEntryModel>();
            foreach (var log in logs)
            {
                securityPrepare?.Invoke(log);
                included.Add(log);
            }

            var paging = filters.Paging;
            var page = included.Skip(paging.PageSize * paging.PageIndex).Take(paging.PageSize).ToList();

            return new PagedResults<AuditLogEntryModel>
            {
                Rows = page,
                TotalMatches = included.Count
            };

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

        protected override Task<AuditLogEntry> ToEntity(AuditLogEntryModel model)
        {
            return Task.FromResult(model.ToEntity());
        }

        protected override Task<AuditLogEntryModel> ToModel(AuditLogEntry entity, AuditLogEntryIdentifier identifier)
        {
            return Task.FromResult(entity.ToModel());
        }

        protected override void UpdateEntity(AuditLogEntry entity, AuditLogEntryModel model)
        {
            throw new NotImplementedException();
        }

        protected override Expression<Func<AuditLogEntry, bool>> WhereClause(AuditLogEntryIdentifier identifier)
        {
            return (a => a.OrganizationKey  == identifier.OrganizationKey
                && a.AuditLogEntryID == identifier.AuditLogID);
        }

        protected override Task GenerateUniqueIdentifier(AuditLogEntryIdentifier identifier)
        {
            identifier.AuditLogID = null;

            return Task.FromResult(0);
        }

        protected override string[] IncludedFields()
        {
            return new string[0];
        }

        protected override void AfterInsert(AuditLogEntry entity, AuditLogEntryIdentifier identifier)
        {
            identifier.AuditLogID = entity.AuditLogEntryID;
        }
    }
}
