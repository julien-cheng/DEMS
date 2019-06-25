namespace Documents.Queues.Tasks.ToPDF.Configuration
{
    using Documents.Queues.Tasks.Configuration;

    public class ToPDFConfiguration : TaskConfiguration
    {
        public string UnoConvUri { get; set; }

        public override string SectionName => "DocumentsQueuesTasksToPDF";
    }
}
