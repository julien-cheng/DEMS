namespace Documents.Clients.Manager.Controllers
{
    using Documents.Clients.Manager.Exceptions;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ControllerBaseCRUD<TModel, TIdentifierModel> : ManagerControllerBase
    {
        // things to override
        protected virtual Task<TModel> UpsertOneAsync(TModel model, CancellationToken cancellationToken) { throw new UnsupportedException(); }
        protected virtual Task<TModel> QueryOneAsync(TIdentifierModel identifier, CancellationToken cancellationToken) { throw new UnsupportedException(); }
        protected virtual Task DeleteOneAsync(TIdentifierModel identifier, CancellationToken cancellationToken) { throw new UnsupportedException(); }
        protected virtual Task<IEnumerable<TModel>> QueryAllAsync(CancellationToken cancellationToken) { throw new UnsupportedException(); }

        // actions
        [HttpGet("all")]
        public virtual Task<IEnumerable<TModel>> List(
            CancellationToken cancellationToken
            ) => QueryAllAsync(cancellationToken);

        [HttpPut]
        public virtual Task<TModel> Upsert(
            [FromBody] TModel model,
            CancellationToken cancellationToken
            ) => UpsertOneAsync(model, cancellationToken);

        [HttpGet]
        public virtual Task<TModel> Get(
            TIdentifierModel identifier,
            CancellationToken cancellationToken
            ) => QueryOneAsync(identifier, cancellationToken);

        [HttpDelete]
        public virtual Task Delete(
            TIdentifierModel identifier
            ) => DeleteOneAsync(identifier, default(CancellationToken));
    }
}