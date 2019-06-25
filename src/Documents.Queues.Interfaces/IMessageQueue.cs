namespace Documents.Queues.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMessageQueue
    {
        object CreateContext(string jsonConfiguration);

        Task<IMessage> PostMessageAsync(object context, string queue, IMessage message);
        Task PostMessageAsync(object context, string queue, IEnumerable<IMessage> messages);

        Task<ISubscription> Subscribe(object context, string queue, 
            Func<IMessage, Task> onMessage,
            Func<string, Task> onError, 
            bool isCallback = false);

        Task<IMessage> PostCallbackAsync(object context, string callback, IMessage message);

        Task<IEnumerable<QueueStatus>> QueryStatus(object context);

        Task<bool> CheckHealthAsync(object context);
    }
}