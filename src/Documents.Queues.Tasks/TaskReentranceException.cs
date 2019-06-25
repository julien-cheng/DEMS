namespace Documents.Queues.Tasks
{
    using System;

    public class TaskReentranceException : Exception
    {
        public TaskReentranceException()
            : base($"Task already completed")
        { }
    }
}
