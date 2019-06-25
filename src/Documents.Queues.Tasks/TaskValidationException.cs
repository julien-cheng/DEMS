namespace Documents.Queues.Tasks
{
    using System;

    public class TaskValidationException : Exception
    {
        public TaskValidationException(string problem)
            : base($"Task validation failed: {problem}")
        { }
    }
}
