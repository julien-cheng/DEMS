namespace Documents.Queues.Tasks.Synchronize.MetadataModels
{
    public class SynchronizeConfiguration
    {
        public const string METADATA_KEY = "synchronize";

        public string ConnectionString { get; set; }

        public int CountyID { get; set; }
        public int? LastChangeLogID { get; set; }
        public int? LastAccountChangeLogID { get; set; }
    }
}
