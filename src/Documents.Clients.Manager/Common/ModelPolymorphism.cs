namespace Documents.Clients.Manager.Common
{
    using Documents.Clients.Manager.Exceptions;
    using Documents.Clients.Manager.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;

    public class ModelPolymorphism : JsonCreationConverter<ModelBase>
    {
        protected override ModelBase Create(Type objectType, JObject jObject)
        {
            var allTypes = ModelBase
                .GetKnownTypes();

            var typeName = jObject.Value<string>("type")
                ?? jObject.Value<string>("Type");

            var type = allTypes
                .Where(t => t.Name == typeName)
                .FirstOrDefault();

            if (type == null)
                throw new UnknownModelType();

            return Activator.CreateInstance(type) as ModelBase;
        }
    }

}
