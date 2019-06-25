namespace Documents.Queues.Tasks.VoiceBase.Models
{
    using System.Collections.Generic;

    public class Media
    {
        public string MediaID { get; set; }
        public string Status { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
