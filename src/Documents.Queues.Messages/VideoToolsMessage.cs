namespace Documents.Queues.Messages
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class VideoToolsMessage : FileBasedMessage
    {
        public VideoToolsMessage() { }
        public VideoToolsMessage(FileIdentifier identifier)
        {
            this.Identifier = identifier;
        }

        public ClippingDetails Clipping { get; set; }
        public ExportFrameDetails Frame { get; set; }
        public WatermarkingDetails Watermark { get; set; }

        public string OutputName { get; set; }

        public string Path { get; set; }

        public class ExportFrameDetails
        {
            public int StartTimeMS { get; set; }
        }

        public class ClippingDetails
        {
            public int StartTimeMS { get; set; }
            public int EndTimeMS { get; set; }

            public List<MutedRange> MutedRanges { get; set; }

            public class MutedRange
            {
                public int StartTimeMS { get; set; }
                public int EndTimeMS { get; set; }
            }
        }

        public class WatermarkingDetails
        {
            public FileIdentifier Watermark { get; set; }

        }
    }
}
