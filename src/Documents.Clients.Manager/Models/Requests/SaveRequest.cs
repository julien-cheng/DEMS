namespace Documents.Clients.Manager.Models.Requests
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using Newtonsoft.Json;

    [JsonConverter(typeof(ModelPolymorphism))]
    public class SaveRequest : ModelBase
    {
        public FolderIdentifier FolderIdentifier { get; set; }
        public FileIdentifier FileIdentifier { get; set; }
    }
}
