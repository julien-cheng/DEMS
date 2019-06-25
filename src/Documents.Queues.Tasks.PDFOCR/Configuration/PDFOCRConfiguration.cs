namespace Documents.Queues.Tasks.PDFOCR.Configuration
{
    using Documents.Queues.Tasks.Configuration;

    public class PDFOCRConfiguration : TaskConfiguration
    {
        public string Executable { get; set; }
        public string Arguments { get; set; }

        public int MaximumSecondsPerPage { get; set; } = 60;

        public override string SectionName => "DocumentsQueuesTasksPDFOCR";
    }
}
