namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Events;
    using Documents.Store;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class ModelControllerBase<TModel, TIdentifier, TStore> : APIControllerBase
        where TStore: IModelStore<TModel, TIdentifier>
        where TModel : class, IHasIdentifier<TIdentifier>
        where TIdentifier : IIdentifier
    {
        protected readonly TStore Store;
        protected readonly IEventSender EventSender;

        protected virtual EventBase EventGet(TModel model) => null;
        protected virtual EventBase EventPut(TModel model) => null;
        protected virtual EventBase EventPost(TModel model) => null;
        protected virtual EventBase EventDelete(TIdentifier identifier) => null;

        public ModelControllerBase(
            TStore store,
            IEventSender eventSender,
            ISecurityContext securityContext
        ) : base(securityContext)
        {
            this.Store = store;
            this.EventSender = eventSender;
        }

        [HttpGet]
        public virtual async Task<TModel> Get(TIdentifier identifier, List<PopulationDirective> population)
        {
            var model = await Store.GetOneAsync(identifier, population ?? DefaultPopulationRelationships);

            if (model == null)
                ExceptionlessStatusCode = System.Net.HttpStatusCode.NotFound;

            await FireEvent(EventGet(model), identifier);

            SetETag(model);
            return model;
        }

        [HttpDelete]
        public virtual async Task<TIdentifier> Delete(TIdentifier identifier)
        {
            await Store.DeleteAsync(identifier);
            await FireEvent(EventDelete(identifier), identifier);
            return identifier;
        }

        [HttpPost]
        public virtual async Task<TModel> Post([FromBody]TModel model)
        {
            model = await Store.InsertAsync(model);
            await FireEvent(EventPost(model), model.Identifier);

            SetETag(model);
            return model;
        }

        [HttpPut]
        public virtual async Task<TModel> Put([FromBody]TModel model)
        {
            string etag = Request.Headers["If-Match"].FirstOrDefault();

            // it's wrapped in quotes and for some reason parsing gives up there
            if (etag != null && etag.StartsWith("\"") && etag.EndsWith("\"") && etag.Length >= 2)
            {
                etag = etag.Substring(1, etag.Length - 2);
                if (model is IProvideETag && model != null)
                    ((IProvideETag)model).ETag = etag;
            }

            model = await Store.UpdateAsync(model);
            await FireEvent(EventPut(model), model.Identifier);

            SetETag(model);
            return model;
        }

        protected void SetETag(TModel model)
        {
            var etag = (model as IProvideETag)?.ETag;
            if (etag != null)
                Response.Headers["ETag"] = $"\"{etag}\"";
        }

        protected virtual async Task FireEvent(EventBase e, TIdentifier identifier)
        {
            if (e != null)
            {
                if (e is IHasIdentifier<TIdentifier>)
                    ((IHasIdentifier<TIdentifier>)e).Identifier = identifier;

                await EventSender.SendAsync(e);
            }
        }

        protected virtual IEnumerable<PopulationDirective> DefaultPopulationRelationships => null;
    }
}
