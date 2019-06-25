namespace Documents.Clients.Manager.Common.Subtitles
{
    using System;
    using System.Collections.Generic;

    public class SubtitleSegmentModel
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int SegmentIndex { get; set; } // one based
        public List<string> Lines { get; set; }
    }
}
