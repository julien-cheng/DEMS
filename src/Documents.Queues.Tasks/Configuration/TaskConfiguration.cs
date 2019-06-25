namespace Documents.Queues.Tasks.Configuration
{
    using Documents.Common;

    public class TaskConfiguration : IDocumentsConfiguration
    {
        public DocumentsAPIConfiguration API { get; set; }
        public virtual string SectionName { get; }

        // how many milliseconds to delay from process entry before
        // starting up. This is to help debug scenarios, let Documents.API start first.
        public int StartupDelay {get; set;}

        public virtual int ConcurrentInstances { get; set; } = 1;

        public bool LogCompletion { get; set; } = true;

        public long MaximumInputFileSize { get; set; } = 0;
        public string OutputViewName { get; set; } = null;

        public bool UnhandledExceptionsFatal { get; set; } = true;

        public int RestartDelay { get; set; } = 15000;
    }
}
