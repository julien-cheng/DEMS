namespace Documents.Clients.Manager.Models.ViewSets
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class DocumentSet : BaseSet
    {
        public enum DocumentTypeEnum
        {
            Unknown,
            PDF
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public DocumentTypeEnum DocumentType { get; set; } = DocumentTypeEnum.Unknown;

        public FileIdentifier FileIdentifier { get; set; }
    }
}
