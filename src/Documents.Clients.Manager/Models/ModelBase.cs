namespace Documents.Clients.Manager.Models
{
    using Documents.Clients.Manager.Common;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    [JsonConverter(typeof(ModelPolymorphism))]
    [KnownType("GetKnownTypes")]

    public class ModelBase
    {
        public string Type { get => this.GetType().Name; set { } }

        public static Type[] GetKnownTypes()
        {
            return Assembly.GetEntryAssembly().DefinedTypes
                .Select(ti => ti.AsType())
                .Where(t => typeof(ModelBase).IsAssignableFrom(t))
                .ToArray();
        }
    }

}
