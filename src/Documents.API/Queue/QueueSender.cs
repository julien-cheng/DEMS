namespace Documents.API.Queue
{
    using Documents.API.Common.Events;
    using Documents.Queues.Interfaces;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class QueueSender : IQueueSender
    {
        private object Context = null;

        public QueueSender(DocumentsAPIConfiguration documentsAPIConfiguration)
        {
            var configurationJSON = $"{{\"AMQPUri\": \"{documentsAPIConfiguration.QueueURI}\", \"ManagementUri\": \"{documentsAPIConfiguration.QueueManagementURI}\"}}";
            var driverTypeName = "Documents.Queues.RabbitMQ.Driver.MessageQueue, Documents.Queues.RabbitMQ.Driver";
            Context = QueueConnectionPool.GetContext(configurationJSON, driverTypeName);

        }

        public Task<IMessage> SendAsync(string queueName, string payload)
            => SendAsync(queueName, new SimpleMessage
            {
                Message = payload
            });

        public async Task<IMessage> SendAsync(string queueName, IMessage message)
        {
            return await QueueConnectionPool.Queue.PostMessageAsync(Context, queueName, message);
        }

        public async Task SendAsync(string queueName, IEnumerable<IMessage> messages)
        {
            await QueueConnectionPool.Queue.PostMessageAsync(Context, queueName, messages);
        }

        public async Task SendEventAsync<T>(IEnumerable<T> eventModels)
            where T : EventBase
        {
            await QueueConnectionPool.Queue.PostMessageAsync(
                Context,
                "EventRouter",
                eventModels.Select(e => new SimpleMessage
                {
                    Message = JsonConvert.SerializeObject(e)
                })
            );
        }

        public async Task<IMessage> SendEventAsync<T>(T eventModel)
            where T : EventBase
        {
            var json = JsonConvert.SerializeObject(eventModel);

            return await QueueConnectionPool.Queue.PostMessageAsync(
                Context,
                "EventRouter",
                new SimpleMessage
                {
                    Message = json
                }
            );
        }


        Task IQueueSender.Initialize()
        {
            // getting this far means we connected already
            // but we've managed to cause the first connection to be the only one 
            // in parallel by initializing from startup
            // this has performance implications for rabbitmq

            return Task.FromResult(0);
        }

        public async Task<IEnumerable<Common.Models.QueueStatus>> GetStatus()
        {
            var list = await QueueConnectionPool.Queue.QueryStatus(Context);
            return list.Select(q => new Common.Models.QueueStatus
            {
                Name = q.Name,
                Connected = q.Connected,
                Outstanding = q.Outstanding,
                Length = q.Length,
                RetryLength = q.RetryLength,
                FailureLength = q.FailureLength,
            });
        }


        Task<bool> IQueueSender.CheckHealthAsync()
        {
            return QueueConnectionPool.Queue.CheckHealthAsync(Context);
        }
    }
}
