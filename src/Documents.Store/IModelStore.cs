namespace Documents.Store
{
    using System;
    using Documents.API.Common.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IModelStore<TModel, TIdentifier>
    {
        Task<PagedResults<TModel>> LoadRelatedToAsync<TRelatedModel>(
            TRelatedModel related,
            PopulationDirective filters,
            IEnumerable<PopulationDirective> populateRelationships,

            Action<TModel> securityPrepare = null
        );

        Task<TModel> GetOneAsync(TIdentifier identifier,
            IEnumerable<PopulationDirective> populateRelationships = null
        );

        Task<TModel> InsertAsync(TModel model);
        Task<TModel> UpdateAsync(TModel model);
        Task DeleteAsync(TIdentifier identifier);
    }
}