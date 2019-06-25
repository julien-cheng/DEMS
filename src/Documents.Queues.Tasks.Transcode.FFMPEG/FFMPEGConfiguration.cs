namespace Documents.Queues.Tasks.Transcode.FFMPEG
{
    using Documents.Queues.Tasks.Configuration;
    using System.Collections.Generic;

    public class FFMPEGConfiguration : TaskConfiguration
    {
        public string Executable { get; set; }
        public Dictionary<string, List<Step>> Tasks { get; set; }

        public override string SectionName => "DocumentsQueuesTasksTranscodeFFMPEG";

        public class Step
        {
            public string Description { get; set; }
            public string Extension { get; set; }
            public string NewName { get; set; }
            public string ContentType { get; set; }

            public string Arguments { get; set; }
        }
    }
}
