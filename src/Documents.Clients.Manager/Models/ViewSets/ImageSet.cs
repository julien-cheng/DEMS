namespace Documents.Clients.Manager.Models.ViewSets
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class ImageSet : BaseSet
    {
        public enum ImageTypeEnum
        {
            Unknown,
            Image
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public ImageTypeEnum ImageType { get; set; } = ImageTypeEnum.Unknown;

        public FileIdentifier FileIdentifier { get; set; }
        public FileIdentifier PreviewImageIdentifier { get; set; }
    }
}
