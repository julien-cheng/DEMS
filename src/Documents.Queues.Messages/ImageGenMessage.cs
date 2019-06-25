namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class ImageGenMessage : FileBasedMessage
    {
        //We want this to get put back and forth in json as an easy to read string.
        [JsonConverter(typeof(StringEnumConverter))]
        public ImageFormatEnum Format { get; set; }

        public string Name { get; set; }

        // Max height, and max width will be used, if they are both specified we'll only scale based 
        // you can specify both, and we'll scale it down to the smaller.
        public int? MaxHeight {get; set;}
        public int? MaxWidth { get; set; }

        // Positive or negative degrees are supported.  Positive will rotate in clockwise.
        // Negative will rotate counterclockwise.
        public float? RotationDegrees { get; set; }

        // The resize percentage will only be used if MaxHeight/MaxWidth is not set
        public double? ResizePercentage { get; set; }
        
        // Quality will only be used if the format is of type jpeg.
        public int? Quality { get; set; }

        //Do you want to convert the image to greyscale?
        public bool? IsGreyscale { get; set; }

        // Here this size type should be something like thumbnail, small preview, icon, etc.
        public string AlternativeViewSizeType { get; set; }

        // These should be more describing the kind of file.  For instance, Image, PDF, Screencap etc.
        public string AlternativeViewType { get; set; }
    }
}
