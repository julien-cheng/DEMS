namespace Documents.API.Client
{
    public class APIEndpoint
    {
        public APIEndpoint(string endpoint)
        {
            Endpoint = endpoint;
        }

        public virtual string Endpoint { get; private set; }

        public static APIEndpoint User { get => new APIEndpoint("/api/v1/user"); }
        public static APIEndpoint UserAuthenticate { get => new APIEndpoint("/api/v1/user/authenticate"); }
        public static APIEndpoint UserImpersonate { get => new APIEndpoint("/api/v1/user/impersonate"); }
        public static APIEndpoint UserPassword { get => new APIEndpoint("/api/v1/user/password"); }
        public static APIEndpoint UserAccessIdentifiers { get => new APIEndpoint("/api/v1/user/accessidentifiers"); }

        public static APIEndpoint Organization { get => new APIEndpoint("/api/v1/organization"); }
        public static APIEndpoint OrganizationAll { get => new APIEndpoint("/api/v1/organization/all"); }

        public static APIEndpoint Folder { get => new APIEndpoint("/api/v1/folder"); }

        public static APIEndpoint File { get => new APIEndpoint("/api/v1/file"); }
        public static APIEndpoint FileMove { get => new APIEndpoint("/api/v1/file/move"); }

        public static APIEndpoint FileUploadChunkSize { get => new APIEndpoint("/api/v1/filecontents/chunksize"); }
        public static APIEndpoint FileUploadBegin { get => new APIEndpoint("/api/v1/filecontents/begin"); }
        public static APIEndpoint FileUploadChunk { get => new APIEndpoint("/api/v1/filecontents"); }
        public static APIEndpoint FileUploadEnd { get => new APIEndpoint("/api/v1/filecontents/end"); }
        public static APIEndpoint FileDownload { get => new APIEndpoint("/api/v1/filecontents"); }
        public static APIEndpoint FileTag { get => new APIEndpoint("/api/v1/filecontents/tag"); }
        public static APIEndpoint FileOnlineStatus { get => new APIEndpoint("/api/v1/filecontents/online"); }

        public static APIEndpoint Search { get => new APIEndpoint("/api/v1/search"); }

        public static APIEndpoint Queue { get => new APIEndpoint("/api/v1/queue"); }
        public static APIEndpoint QueueStatus { get => new APIEndpoint("/api/v1/queue/status"); }
        public static APIEndpoint QueueBatch { get => new APIEndpoint("/api/v1/queue/batch"); }
        public static APIEndpoint QueueConnect { get => new APIEndpoint("/api/v1/queue/connect"); }
        public static APIEndpoint QueueCreateCallback { get => new APIEndpoint("/api/v1/callback/create"); }

        public static APIEndpoint AuditLogQuery { get => new APIEndpoint("/api/v1/auditlogentry/all"); }
        public static APIEndpoint AuditLog { get => new APIEndpoint("/api/v1/auditlogentry"); }


        public static APIEndpoint HealthGet { get => new APIEndpoint("/api/v1/health"); }
    }
}