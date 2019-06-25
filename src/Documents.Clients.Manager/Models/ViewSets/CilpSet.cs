namespace Documents.Clients.Manager.Models.ViewSets
{
    using System.Collections.Generic;

    public class ClipSet : MediaSet
    {
        public IEnumerable<ClipSegmentModel> Segments { get; set; }
        public string CanonicalURLFormat { get; set; } = null;
    }
}
