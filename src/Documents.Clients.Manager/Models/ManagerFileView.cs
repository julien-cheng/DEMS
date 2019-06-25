namespace Documents.Clients.Manager.Models
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class ManagerFileView : ModelBase
    {
        public enum ViewerTypeEnum
        {
            Document,
            Text,
            Image,
            Audio,
            Video,
            Transcript,
            Clip,
            Offline,
            Unknown
        }

        public string[] Icons { get; set; }
        public FileIdentifier Identifier { get; set; }
        public string Label { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ViewerTypeEnum ViewerType { get; set; }
    }
}