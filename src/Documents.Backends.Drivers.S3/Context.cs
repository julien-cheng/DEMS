namespace Documents.Backends.Drivers.S3
{
    using Amazon;
    using Amazon.Runtime;
    using Amazon.S3;
    using Newtonsoft.Json;

    public class Context
    {
        public string AWSAccessKeyID { get; set; }
        public string AWSSecretAccessKey { get; set; }
        public string BucketName { get; set; }
        public string AWSRegion { get; set; }

        internal AmazonS3Client S3 {get; set;}

        internal void Configure(string json)
        {
            JsonConvert.PopulateObject(json, this);

            S3 = new AmazonS3Client(
                new BasicAWSCredentials(
                    AWSAccessKeyID,
                    AWSSecretAccessKey
                ),
                new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(AWSRegion),
                    BufferSize = 65536
                }
            );            
        }
    }
}
