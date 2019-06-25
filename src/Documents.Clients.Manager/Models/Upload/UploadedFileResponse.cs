namespace Documents.Clients.Manager.Models.Upload
{
    using Documents.API.Common.Models;

    public class UploadedFileResponse
    {
        public string Token { get; set; }
        public FileIdentifier FileIdentifier { get; set; }
    }
}
