namespace Documents.Queues.Tasks.TextExtract.Configuration
{
    using Documents.Queues.Tasks.Configuration;
    using System.Collections.Generic;

    public class TextExtractConfiguration : TaskConfiguration
    {
        public List<Step> Steps { get; set; }

        public bool OCRPDFsIfNoText { get; set; } = true;

        public override string SectionName => "DocumentsQueuesTasksTextExtract";
    }
}
