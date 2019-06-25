namespace Documents.Clients.Manager.Models.ViewSets
{
    using System.Collections.Generic;

    public class TranscriptSet : MediaSet
    {
        public IEnumerable<SegmentModel> Segments { get; set; }
    }
}
