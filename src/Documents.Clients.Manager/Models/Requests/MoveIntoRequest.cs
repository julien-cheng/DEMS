namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Newtonsoft.Json;

    [JsonConverter(typeof(ModelPolymorphism))]
    public class MoveIntoRequest : ModelBase
    {
        // one of the two of these will be populated, the other null
        public FileIdentifier SourceFileIdentifier { get; set; }
        public PathIdentifier SourcePathIdentifier { get; set; }

        public PathIdentifier TargetPathIdentifier { get; set; }
    }
}
