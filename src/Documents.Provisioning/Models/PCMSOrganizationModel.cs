namespace Documents.Provisioning.Models
{
    public class PCMSOrganizationModel
    {
        public string CountyName { get; set; }
        public int CountyID { get; set; }
        public string UseOrganizationKey { get; set; }

        public string MasterEncryptionKey { get; set; }
        public string AWSS3Region { get; set; } = "us-east-1";
        public string AWSS3BucketName { get; set; }
        public string AWSSecretAccessKey { get; set; }
        public string AWSAccessKeyID { get; set; }
        public string PCMSBridgeUserPassword { get; set; }

        public bool? EDiscoveryActive { get; set; }
        public bool? LEOActive { get; set; }

        public bool OverrideBackendConfiguration { get; set; }

        public string PCMSDBConnectionString { get; set; } 
            = "Server=PCMSShare-DB.internal.nypti.org; Database=PCMS; Integrated Security=sspi;Min Pool Size=20;Max Pool Size=200;Pooling=true;";
    }
}
