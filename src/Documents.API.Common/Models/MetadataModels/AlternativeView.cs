namespace Documents.API.Common.Models.MetadataModels
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class AlternativeView
    {
        public FileIdentifier FileIdentifier { get; set; }

        public string MimeType { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ImageFormatEnum? ImageFormat { get; set; }

        public int? Height { get; set; }
        public int? Width { get; set; }

        public int? Quality { get; set; }
        public bool? IsGreyscale { get; set; }

        // Size type should be something more like Thumbnail, small preview etc.
        public string SizeType { get; set; }

    }
}
