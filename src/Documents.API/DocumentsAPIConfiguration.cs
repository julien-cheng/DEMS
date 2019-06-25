namespace Documents.API
{
    using Documents.Common.WebHost;
    using System;
    using Documents.Common;

    public class DocumentsAPIConfiguration : IDocumentsWebConfiguration
    {
        public string TokenValidationSecret { get; set; }
        public string TokenIssuer { get; set; }
        public string TokenAudience { get; set; }
        public int TokenExpirationSeconds { get; set; }

        public string QueueURI { get; set; }
        public string QueueManagementURI { get; set; } = "http://guest:guest@localhost:15672";
        
        public int ReconnectStaleSubscriptionSecondsAverage { get; set; } = 600;

        public Uri ElasticSearchUri { get; set; }
        public string ElasticSearchIndex { get; set; }
        public string HostingURL { get; set; } = "http://*:5001/";

        public string BackendGatewayURL { get; set; }

        public string APITokenKey { get; set; }
        public string CallbackURL { get; set; }

        public string ConnectionString { get; set; }

        public bool RedisCacheEnabled { get; set; } = false;
        public string RedisConnection { get; set; } = "localhost:6379";
        public string RedisInstanceName { get; set; } = null;
        public bool AuditingEnabled { get; set; } = true;
        public string AuditingExclusion { get; set; } = null;

        string IDocumentsConfiguration.SectionName => "DocumentsAPI";
    }
}
