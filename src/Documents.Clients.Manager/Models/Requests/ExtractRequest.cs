namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Newtonsoft.Json;

    [JsonConverter(typeof(ModelPolymorphism))]
    public class ExtractRequest : ModelBase
    {
        public FileIdentifier FileIdentifier;
    }
}
