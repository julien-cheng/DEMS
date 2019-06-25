using System.Collections.Generic;

namespace Documents.Queues.Tasks.VoiceBase.Models
{
    public class MediaResponse
    {
        public string MediaID { get; set; }
        public string Status { get; set; }
        public int Length { get; set; }

        public TranscriptModel Transcript { get; set; }

        public class TranscriptModel
        {
            public double Confidence { get; set; }
            public IEnumerable<AlternateFormat> AlternateFormats { get; set; }

            public class AlternateFormat
            {
                public string Format { get; set; }
                public string ContentType { get; set; }
                public string ContentEncoding { get; set; }
                public string Charset { get; set; }

                public string Data { get; set; }
            }
        }
    }
}
