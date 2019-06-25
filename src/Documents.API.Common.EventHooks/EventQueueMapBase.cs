namespace Documents.API.Common.EventHooks
{
    using Documents.API.Client;
    using Documents.API.Common.Events;
    using System;
    using System.Threading.Tasks;

    public abstract class EventQueueMapBase
    {
        public abstract Task<bool> HandleEventAsync(
            EventBase incomingEvent,
            Func<string, object, Task> enqueue,
            Connection api
        );
    }
}
