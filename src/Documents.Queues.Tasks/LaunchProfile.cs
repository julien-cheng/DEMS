namespace Documents.Queues.Tasks
{
    using System;

    public class LaunchProfile
    {
        public Type ApplicationType { get; set; }
        public int? FixedInstanceCount { get; set; }
    }
}
