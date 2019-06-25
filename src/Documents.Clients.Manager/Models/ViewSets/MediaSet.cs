namespace Documents.Clients.Manager.Models.ViewSets
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;

    public class MediaSet : BaseSet
    {
        public enum MediaTypeEnum
        {
            Unknown,
            Video,
            Audio
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public MediaTypeEnum MediaType { get; set; } = MediaTypeEnum.Unknown;

        public bool AutoPlay { get; set; }
        public bool Preload { get; set; }

        public FileIdentifier Poster { get; set; }

        public IEnumerable<MediaSource> Sources { get; set; }
        public IEnumerable<MediaSubtitles> Subtitles { get; set; }
    }
}
