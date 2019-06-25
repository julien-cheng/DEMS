namespace Documents.Clients.Manager.Services
{
    using Common;
    using Documents.Clients.Manager.Exceptions;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ServiceBase<TModel, TIdentifierModel>
    {
        protected readonly APIConnection Connection;

        public ServiceBase(APIConnection connection)
        {
            Connection = connection;
        }

        public virtual Task<TIdentifierModel> DeleteOneAsync(
            TIdentifierModel identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => throw new UnsupportedException();

        public virtual Task<TModel> UpsertOneAsync(
            TModel model,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => throw new UnsupportedException();

        public virtual Task<TModel> QueryOneAsync(
            TIdentifierModel identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => throw new UnsupportedException();

        public virtual Task<IEnumerable<TModel>> QueryAllAsync(
            CancellationToken cancellationToken = default(CancellationToken)
        ) => throw new UnsupportedException();
    }
}