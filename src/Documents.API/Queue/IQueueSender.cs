namespace Documents.API.Queue
{
    using Documents.API.Common.Events;
    using Documents.Queues.Interfaces;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IQueueSender
    {
        Task<IMessage> SendAsync(string queueName, IMessage message);
        Task<IMessage> SendAsync(string queueName, string payload);
        Task<IMessage> SendEventAsync<T>(T eventModel) where T : EventBase;
        Task SendEventAsync<T>(IEnumerable<T> eventModels) where T : EventBase;
        Task Initialize();
        Task<bool> CheckHealthAsync();
    }
}