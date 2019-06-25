namespace Documents.API.Client
{
    public class DownloadHeaderInformation
    {
        public long RangeFrom { get; set; }
        public long RangeTo { get; set; }
        public long RangeLength { get; set; }

        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public string FileName { get; set; }
    }
}
