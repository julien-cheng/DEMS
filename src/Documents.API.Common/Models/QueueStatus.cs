namespace Documents.API.Common.Models
{
    public class QueueStatus
    {
        public string Name { get; set; }
        public int Connected { get; set; }
        public int Outstanding { get; set; }
        public int Length { get; set; }
        public int RetryLength { get; set; }
        public int FailureLength { get; set; }
    }
}
