namespace Documents.Store.SqlServer.Stores
{
    using BCrypt.Net;
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Store.Exceptions;
    using Documents.Store.SqlServer.Entities;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Security;
    using System.Threading.Tasks;

    public class UserStore : StoreBase<UserModel, UserIdentifier, User>, IUserStore
    {
        public UserStore(ISecurityContext securityContext, DocumentsContext database, IServiceProvider serviceProvider)
            : base(securityContext, database, serviceProvider)
        {
        }

        private async Task<bool> OrganizationPrivilegeCheck(OrganizationIdentifier identifier, string privilegeName)
        {
            return (
                    await GetStore<IOrganizationStore>()
                        .GetOneAsync(identifier)
                )
                .PrivilegeCheck(privilegeName, SecurityContext);
        }

        protected override Task PrivilegeCheckCreate(UserIdentifier identifier)
            => OrganizationPrivilegeCheck(identifier, "user:create");

        protected override Task PrivilegeCheckDelete(UserModel model)
            => OrganizationPrivilegeCheck(model.Identifier, "user:delete");

        protected override Task PrivilegeCheckWrite(UserModel model)
            => OrganizationPrivilegeCheck(model.Identifier, "user:write");

        protected override Task PrivilegeCheckRead(UserModel model)
            => OrganizationPrivilegeCheck(model.Identifier, "user:read");

        public async Task<UserModel> PasswordVerifyAsync(UserIdentifier identifier, string password)
        {
            return await WithOneAsync(identifier, pair =>
            {
                if (BCrypt.Verify(password, pair.Entity.UserSecretHash))
                    return pair.Model;
                else
                    throw new SecurityException();
            });
        }

        protected override string CacheKey(UserIdentifier identifier)
        {
            return $"user {identifier.OrganizationKey}/{identifier.UserKey}";
        }

        public async Task<UserModel> PasswordSetAsync(UserIdentifier identifier, string password)
        {
            // if we're trying to set the password of a user that is NOT the currently
            // logged in user, then check the organization for permission to do so
            if (!SecurityContext.UserIdentifier.Equals(identifier.UserKey))
                await OrganizationPrivilegeCheck(identifier, "user:credentials");

            return await UpdateOneAsync(identifier, pair =>
            {
                pair.Entity.UserSecretHash = BCrypt.HashPassword(password);

                return pair.Model;
            });
        }

        public async Task<UserModel> AccessIdentifiersSetAsync(UserIdentifier identifier, IEnumerable<string> userAcessIdentifiers)
        {
            await OrganizationPrivilegeCheck(identifier, "user:identifiers");

            var model = await UpdateOneAsync(identifier, pair =>
            {
                pair.Entity.UserAccessIdentifiers = userAcessIdentifiers.Select(i => new UserAccessIdentifier
                {
                    Identifier = i
                }).ToList();

                return pair.Model;
            });

            return model;
        }

        public override async Task<PagedResults<UserModel>> LoadRelatedToAsync<TRelatedModel>(TRelatedModel related, PopulationDirective filters, IEnumerable<PopulationDirective> populateRelationships, Action<UserModel> securityPrepare)
        {
            if (typeof(TRelatedModel) == typeof(OrganizationModel))
            {
                var organization = related as OrganizationModel;

                var users = await DoIncludes(Database.User
                    .AsNoTracking())
                    .Where(u =>
                        u.Organization.OrganizationKey == organization.Identifier.OrganizationKey
                    )
                    .Where(u => u.UserKey != null)
                    .Select(u => u.ToModel())
                    .ToListAsync();

                foreach (var user in users)
                    securityPrepare?.Invoke(user);

                var paging = filters.Paging;
                // todo: sort

                return new PagedResults<UserModel>
                {
                    Rows = users.Skip(paging.PageSize * paging.PageIndex).Take(paging.PageSize),
                    TotalMatches = users.Count
                };
            }

            throw new Exception("Unknown relationship type");
        }

        //duplicated in folder
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

        protected async override Task<User> ToEntity(UserModel model)
        {
            return model.ToEntity(await OrganizationIDLookup(model.Identifier));
        }

        protected override Task<UserModel> ToModel(User entity, UserIdentifier identifier)
        {
            return Task.FromResult(entity.ToModel());
        }

        protected override void UpdateEntity(User entity, UserModel model)
        {
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.EmailAddress = model.EmailAddress;

            base.SetEtag(entity, model, nameof(User.UpdateVersion));
        }

        protected override Expression<Func<User, bool>> WhereClause(UserIdentifier identifier)
        {
            return (u => u.UserKey == identifier.UserKey
                && u.Organization.OrganizationKey == identifier.OrganizationKey);
        }

        protected override string[] IncludedFields()
        {
            return new[]
            {
                $"{nameof(User.Organization)}",
                $"{nameof(User.UserAccessIdentifiers)}"
            };
        }

        protected override void SoftDelete(User entity)
        {
            entity.DeletedKey = entity.UserKey;
            entity.UserKey = null;
        }
    }
}
