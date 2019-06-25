namespace Documents.Store.SqlServer.Stores.Models
{
    public class EntityModelPair<TEntity, TModel>
    {
        public EntityModelPair(TEntity entity, TModel model)
        {
            this.Entity = entity;
            this.Model = model;
        }

        public TEntity Entity { get; set; }
        public TModel Model {get;set;}
    }
}
