namespace Documents.Queues.Messages
{
    public class LogReaderMessage
    {
        public ReaderActions Action { get; set; }
     
        public enum ReaderActions
        {
            PatrolUploads,
            //MonthlyAccounting
        }
    }
}
