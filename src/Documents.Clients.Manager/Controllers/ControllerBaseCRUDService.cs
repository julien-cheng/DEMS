namespace Documents.Clients.Manager.Controllers
{
    using Documents.Clients.Manager.Services;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ControllerBaseCRUDService<TModel, TIdentifierModel, TService> : ControllerBaseCRUD<TModel, TIdentifierModel>
        where TService: ServiceBase<TModel, TIdentifierModel>
    {
        protected TService Service;

        protected override Task<TModel> UpsertOneAsync(TModel model, CancellationToken cancellationToken)
            => Service.UpsertOneAsync(model, cancellationToken);

        protected override Task<TModel> QueryOneAsync(TIdentifierModel identifier, CancellationToken cancellationToken)
            => Service.QueryOneAsync(identifier, cancellationToken);

        protected override Task DeleteOneAsync(TIdentifierModel identifier, CancellationToken cancellationToken)
            => Service.QueryOneAsync(identifier, cancellationToken);

        protected override Task<IEnumerable<TModel>> QueryAllAsync(CancellationToken cancellationToken)
            => Service.QueryAllAsync(cancellationToken);

    }
}