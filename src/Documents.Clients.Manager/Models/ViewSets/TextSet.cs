namespace Documents.Clients.Manager.Models.ViewSets
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class TextSet : BaseSet
    {
        public enum TextTypeEnum
        {
            Unknown,
            Text
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public TextTypeEnum TextType { get; set; } = TextTypeEnum.Unknown;

        public FileIdentifier FileIdentifier { get; set; }
    }
}
