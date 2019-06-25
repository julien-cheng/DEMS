namespace Documents.Clients.Manager.Models
{
    public class ClientConfigurationModel : ModelBase
    {
        public bool IsTopNavigationVisible { get; set; }
        public bool IsSearchEnabled { get; set; } = true;

        /// File Upload Options
        public bool AutoUpload { get; set; } = true;
        public bool DisableMultipart { get; set; } = true;
        public bool EnableGlobalChunkedUpload { get; set; } = true;
        public bool RemoveAfterUpload { get; set; } = true;
        public bool ReportProgress { get; set; } = true;
        public string UserTimeZone { get; set; }

        public int ConcurrentUploadLimit { get; set; } = 5;
        public int MaxChunkSize { get; set; } = 10485760;
        public long MaxFileSize { get; set; } = 5368709120;
        public int MaxNumberRetries { get; set; } = 3;
        public int QueueLimit { get; set; } = 50000;
        
        public string DataType { get; set; } = "json";
        public string Method { get; set; } = "POST";

        // filters?: Array<FilterFunction>; 
        // allowedMimeType?: Array<string>;
        // allowedFileType?: Array<string>;
    }
}
