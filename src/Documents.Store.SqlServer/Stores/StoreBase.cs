namespace Documents.Store.SqlServer.Stores
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Common;
    using Documents.Store.Exceptions;
    using Documents.Store.SqlServer.Entities;
    using Documents.Store.SqlServer.Stores.Models;
    using Documents.Store.Utilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class StoreBase<TModel, TIdentifier, TEntity> : IModelStore<TModel, TIdentifier>
        where TModel : class, IHasIdentifier<TIdentifier>
        where TEntity : class
        where TIdentifier: Identifier, new()
    {
        protected readonly DocumentsContext Database = null;

        // so yea... it's understood that this is an anti-pattern, but I'm not going to
        // let DI tell me I can't have circular references between my repos -AL
        protected readonly IServiceProvider ServiceProvider = null;

        protected readonly ISecurityContext SecurityContext;
        protected IDistributedCache Cache = null;

        protected abstract Expression<Func<TEntity, bool>> WhereClause(TIdentifier identifier);
        private readonly string ModelName = typeof(TModel).Name;
        protected readonly ILogger Logger;

        private const int MAX_CACHE_WAIT_MS = 2000;

        public StoreBase(
            ISecurityContext securityContext,
            DocumentsContext database,
            IServiceProvider serviceProvider
        )
        {
            this.SecurityContext = securityContext;
            this.ServiceProvider = serviceProvider;
            this.Database = database;

            Cache = serviceProvider.GetService(typeof(IDistributedCache)) as IDistributedCache;

            this.Logger = Logging.CreateLogger(this.GetType());
        }

        protected virtual Task PrivilegeCheckCreate(TIdentifier identifier) => Task.FromResult(true);
        protected virtual Task PrivilegeCheckWrite(TModel model) => Task.FromResult(true);
        protected virtual Task PrivilegeCheckDelete(TModel model) => Task.FromResult(true);
        protected virtual Task PrivilegeCheckRead(TModel model) => Task.FromResult(true);

        protected virtual string CacheKey(TIdentifier identifier)
        {
            return null;
        }

        // With a single object (model and entity) perform some function
        // synchronous callback flavor
        protected virtual Task<TReturn> WithOneAsync<TReturn>(TIdentifier identifier, Func<EntityModelPair<TEntity, TModel>, TReturn> update, bool tracking = false)
            => WithOneAsync(identifier, pair => Task.FromResult(update(pair)));

        // With a single object (model and entity) perform some function
        // asynchronous callback flavor
        protected async virtual Task<TReturn> WithOneAsync<TReturn>(TIdentifier identifier, Func<EntityModelPair<TEntity, TModel>, Task<TReturn>> updateAsync, bool tracking = false)
        {
            if (identifier == null || !identifier.IsValid)
                throw new StoreException("Identifier is not valid");

            var pair = await GetOneWithEntityAsync(identifier, tracking: tracking);
            if (pair != null)
                return await updateAsync(pair);
            else
                return default(TReturn);
                //throw new StoreException($"{ModelName} does not exist");

        }

        // With a single object (model and entity) get the object for update and save changes (sync flavor)
        protected virtual Task<TReturn> UpdateOneAsync<TReturn>(TIdentifier identifier, Func<EntityModelPair<TEntity, TModel>, TReturn> update)
            => UpdateOneAsync(identifier, pair => Task.FromResult(update(pair)));

        // With a single object (model and entity) get the object for update and save changes (async flavor)
        protected async virtual Task<TReturn> UpdateOneAsync<TReturn>(TIdentifier identifier, Func<EntityModelPair<TEntity, TModel>, Task<TReturn>> updateAsync)
        {
            var response = await WithOneAsync(identifier, updateAsync, tracking: true);

            await Database.SaveChangesAsync();

            await CacheInvalidate(identifier);

            return response;
        }

        protected async Task<bool> ExistsAsync(TIdentifier identifier)
        {
            if (!identifier.IsValid)
                return false;
            else
                return await Database.Set<TEntity>()
                    .Where(WhereClause(identifier))
                    .AnyAsync();
        }

        protected abstract Task<TEntity> ToEntity(TModel model);
        protected abstract Task<TModel> ToModel(TEntity entity, TIdentifier identifier);

        protected abstract string[] IncludedFields();

        protected virtual IQueryable<TEntity> DoIncludes(IQueryable<TEntity> source)
        {
            foreach (var field in IncludedFields())
                source = source.Include(field);

            return source;
        }

        protected virtual Task PopulateRelated(TModel model, TEntity entity, IEnumerable<PopulationDirective> populateRelationships)
        {
            return Task.FromResult(0);
        }

        protected void SetEtag(TEntity entity, TModel model, string entityProperty)
        {
            var etag = (model as IProvideETag)?.ETag;

            if (etag != null)
                Database.Entry(entity).Property(entityProperty).OriginalValue
                    = Convert.FromBase64String(etag);
        }

        // This is an opportunity to perform inheritance from parents into this child model
        public virtual Task ModelInheritance(TModel model, TEntity entity)
        {
            return Task.FromResult(0);
        }

        protected virtual void SoftDelete(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(TIdentifier identifier)
        {
            using (Logger.BeginScope(new { identifier }))
            {
                Logger.LogDebug("DeleteAsync: {0}", identifier);

                await UpdateOneAsync(identifier, async (pair) =>
                {
                    if (pair != null)
                    {
                        await this.PrivilegeCheckDelete(pair.Model);

                        SoftDelete(pair.Entity);

                        //Logger.LogError("Setting state to modified");
                        Database.Entry(pair.Entity).State = EntityState.Modified;
                        //Logger.LogError("Set state to modified");
                    }

                    return pair.Entity;
                });
                await CacheInvalidate(identifier);

                Logger.LogDebug("DeleteAsync: done");
            }
        }

        public virtual Task<PagedResults<TModel>> LoadRelatedToAsync<TRelatedModel>(
            TRelatedModel related, 
            PopulationDirective filters, 
            IEnumerable<PopulationDirective> populateRelationships, 
            Action<TModel> securityPrepare)
        {
            throw new NotImplementedException();
        }


        private async Task<TModel> Cached(TIdentifier identifier, IEnumerable<PopulationDirective> populateRelationships, Func<Task<TModel>> func)
        {
            if (
                Cache == null 
                || populateRelationships != null
                )
                return await func();

            string key = CacheKey(identifier);

            if (key == null)
                return await func();

            TModel model = null;
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(MAX_CACHE_WAIT_MS);
                string json = await Cache.GetStringAsync(key, cts.Token);
                

                if (json != null)
                {
                    model = JsonConvert.DeserializeObject<TModel>(json);

                    if (model != null)
                        return model;
                }
            }
            catch (TaskCanceledException)
            {
                Logger.LogWarning($"Cache took longer than {MAX_CACHE_WAIT_MS}");
                return await func();

            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Cache slow or failed");
                return await func();
            }

            model = await func();

            try
            {
                if (model != null)
                    await Cache.SetStringAsync(key, JsonConvert.SerializeObject(model));

                return model;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Cache failed");
                return model;
            }
        }

        protected async Task CacheInvalidate(TIdentifier identifier)
        {
            if (Cache != null)
            {
                var key = CacheKey(identifier);
                if (key != null)
                    await Cache.RemoveAsync(key);
            }
        }

        public virtual async Task<TModel> GetOneAsync(TIdentifier identifier, IEnumerable<PopulationDirective> populateRelationships = null)
        {
            using (Logger.BeginScope(new { identifier }))
            {
                var model = await Cached(identifier, populateRelationships, async () =>
                {
                    Logger.LogDebug("GetOneAsync: {0} {1}", identifier, ModelName);
                    var pair = await GetOneWithEntityAsync(identifier, populateRelationships);

                    Logger.LogDebug("GetOneAsync: done");

                    return pair?.Model;
                });

                if (model != null)
                    await PrivilegeCheckRead(model);

                return model;
            }
        }

        public virtual async Task<EntityModelPair<TEntity, TModel>> GetOneWithEntityAsync(TIdentifier identifier, IEnumerable<PopulationDirective> populateRelationships = null, bool tracking = false)
        {
            var name = typeof(TEntity).Name;

            //Logger.LogDebug($"{name} GetOneWithEntityAsync() building query");

            var query = DoIncludes(Database.Set<TEntity>());
            if (!tracking)
                query = query.AsNoTracking();

            query = query
                .Where(WhereClause(identifier));

            //Logger.LogDebug($"{name} GetOneWithEntityAsync() about to execute");
            var entity = await query.FirstOrDefaultAsync();
            //Logger.LogDebug($"{name} GetOneWithEntityAsync() query executed");

            if (entity == null)
                return null;

            //Logger.LogDebug($"{name} GetOneWithEntityAsync() ToModel()");
            var model = await ToModel(entity, identifier);

            //Logger.LogDebug($"{name} GetOneWithEntityAsync() Model Inheritance");
            await ModelInheritance(model, entity);

            //Logger.LogDebug($"{name} GetOneWithEntityAsync() Populate Related");
            await PopulateRelated(model, entity, populateRelationships);

            //Logger.LogDebug($"{name} GetOneWithEntityAsync() Done");
            return new EntityModelPair<TEntity, TModel>(entity, model);
        }

        protected abstract void UpdateEntity(TEntity entity, TModel model);

        public async Task<TModel> UpdateAsync(TModel model)
        {
            if (model == null)
                throw new StoreException($"Missing {ModelName}");
            
            using (Logger.BeginScope(new { identifier = model.Identifier }))
            {
                //Logger.LogDebug("UpdateAsync: {0} {1}", model.Identifier, ModelName);

                if (await ExistsAsync(model.Identifier))
                {
                    if (!model.Identifier.IsValid)
                        throw new StoreException($"{ModelName}'s Identifier is not valid");

                    await UpdateOneAsync(model.Identifier, async pair =>
                    {
                        await this.PrivilegeCheckWrite(pair.Model);

                        var set = Database.Set<TEntity>();

                        if (pair.Entity != null)
                        {
                            UpdateEntity(pair.Entity, model);
                            Database.State(pair.Entity, EntityState.Modified);
                        }
                        else
                        {
                            pair.Entity = await ToEntity(model);
                            if (typeof(TEntity) is IHaveAuditDates)
                                ((IHaveAuditDates)pair.Entity).Created = DateTime.UtcNow;

                            set.Add(pair.Entity);
                        }

                        if (typeof(TEntity) is IHaveAuditDates)
                            ((IHaveAuditDates)pair.Entity).Modified = DateTime.UtcNow;


                        return 0; // return something, else async/await gets confused
                    });

                    //Logger.LogDebug("UpdateAsync: calling GetOneAsync");
                    await CacheInvalidate(model.Identifier);
                    return await GetOneAsync(model.Identifier);
                }
                else
                {
                    //Logger.LogDebug("UpdateAsync: calling InsertAsync");
                    return await InsertAsync(model);
                }
            }
        }

        protected virtual async Task GenerateUniqueIdentifier(TIdentifier identifier)
        {
            var iid = (IIdentifier)identifier;

            iid.ComponentKey = await KeyGenerator.GenerateKeyAsync(async candidate =>
            {
                // ensure the generated key is unique
                iid.ComponentKey = candidate;

                return await ExistsAsync(identifier);
            });
        }

        // Insert a new model, with or without a key
        public async Task<TModel> InsertAsync(TModel model)
        {
            if (model == null)
                throw new StoreException($"Missing {ModelName}");


            using (Logger.BeginScope(new { identifier = model.Identifier }))
            {
                //Logger.LogDebug("InsertAsync: {0} {1}", model.Identifier, ModelName);

                // this only really works if it's a non-compound key, like organization
                if (model.Identifier == null)
                    model.Identifier = new TIdentifier();

                // the portion of the identifier that's 1-to-1 with the entity is 
                // not specified, generate one
                var identifier = model.Identifier as IIdentifier;
                if (identifier.ComponentKey == null)
                {
                    await this.PrivilegeCheckCreate(model.Identifier);
                    await this.GenerateUniqueIdentifier(model.Identifier);

                }
                else
                    // doing an insert on a pre-existing object is forbidden
                    if (await ExistsAsync(model.Identifier))
                        throw new StoreException($"{ModelName} already exists");

                // translate the model to an entity. Additional requirements on key
                // population are frequenty in here
                var entity = await ToEntity(model);

                await this.PrivilegeCheckCreate(model.Identifier);

                if (typeof(TEntity) is IHaveAuditDates)
                {
                    ((IHaveAuditDates)entity).Created = DateTime.UtcNow;
                    ((IHaveAuditDates)entity).Modified = DateTime.UtcNow;
                }

                Database.Set<TEntity>().Add(entity);

                //Logger.LogDebug("InsertAsync: saving...");
                await Database.SaveChangesAsync();
                //Logger.LogDebug("InsertAsync: calling GetOneAsync");

                this.AfterInsert(entity, model.Identifier);

                return await this.GetOneAsync(model.Identifier);
            }
        }

        protected virtual void AfterInsert(TEntity entity, TIdentifier identifier)
        {

        }


        // using a Service Locator pattern, find a peer store
        // this is done as service locator to allow circular dependencies between stores
        protected TStore GetStore<TStore>()
            where TStore : class
        {
            return ServiceProvider.GetService(typeof(TStore)) as TStore;
        }
    }
}
