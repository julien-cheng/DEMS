namespace Documents.API.Models
{
    public class ChunkedUploadModel
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
    }
}