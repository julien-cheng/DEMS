namespace Documents.Clients.PCMSBridge.Models
{
    public class UploadRequestModel : AttachmentFile
    {
        public string ContentsBase64 { get; set; }
    }
}