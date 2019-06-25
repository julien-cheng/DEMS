namespace Documents.Clients.Manager.Models.ViewSets
{
    using System.Collections.Generic;

    public class ClipSegmentModel
    {
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public string Text { get; set; }

        public List<MuteSegment> Mutes { get; set; }

        public class MuteSegment
        {
            public int StartTime { get; set; }
            public int EndTime { get; set; }
            public string Text { get; set; }
        }
    }
}
