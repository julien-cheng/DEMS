namespace Documents.Queues.Tasks.LogReader
{
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Configuration;
    using System.Threading.Tasks;

    public class LogReaderTask : 
        QueuedApplication<LogReaderTask.LogReaderConfiguration, LogReaderMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksLogReader";
        protected override string QueueName => "LogReader";

        protected override async Task Process()
        {
            switch (CurrentMessage.Action)
            {
                case LogReaderMessage.ReaderActions.PatrolUploads:
                    await UploadNotify.ScanForUploadBatches(API, Configuration);
                    break;
                /*case LogReaderMessage.ReaderActions.MonthlyAccounting:
                    await TranscriptionReport.CreateReport(API, Configuration);
                    break;*/
            }
        }

        public class LogReaderConfiguration : TaskConfiguration
        {
            public override string SectionName => "DocumentsQueuesTasksLogReader";
            public FolderIdentifier OutputFolder { get; set; }
            public UserIdentifier[] Recipients { get; set; }
            public string NotifyTemplateName { get; set; }
            public string UploadCaseLinkURL { get; set; }
            public long UploadNotifyAfterMS { get; set; } = 10 * 60 * 1000;
            public long UploadLookBackMinutes { get; set; } = 500;
        }
    }
}
