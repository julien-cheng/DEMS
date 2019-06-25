namespace Documents.Queues.RabbitMQ.Driver
{
    using Documents.Queues.Interfaces;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class MessageQueue : IMessageQueue
    {
        object IMessageQueue.CreateContext(string jsonConfiguration)
        {
            var context = new Context(jsonConfiguration);

            return context;
        }

        async Task<IMessage> IMessageQueue.PostMessageAsync(object context, string queue, IMessage message)
        {
            var queueContext = context as Context;
            await queueContext.Enqueue(queue, message);

            return message;
        }

        async Task IMessageQueue.PostMessageAsync(object context, string queue, IEnumerable<IMessage> messages)
        {
            var queueContext = context as Context;
            await queueContext.Enqueue(queue, messages);
        }

        async Task<ISubscription> IMessageQueue.Subscribe(object context, string queue,
            Func<IMessage, Task> onMessage,
            Func<string, Task> onError, 
            bool isCallback)
        {
            var queueContext = context as Context;
            return await queueContext.Subscribe(queue, onMessage, onError, isCallback);
        }

        async Task<IMessage> IMessageQueue.PostCallbackAsync(object context, string callback, IMessage message)
        {
            var queueContext = context as Context;
            await queueContext.PostCallback(callback, message);

            return message;
        }


        private class NativeQueue
        {
            public string Name { get; set; }
            public int Consumers { get; set; }
            public int Messages_Unacknowledged { get; set; }
            public int Messages_Ready { get; set; }
        }

        async Task<IEnumerable<QueueStatus>> IMessageQueue.QueryStatus(object context)
        {
            var ctx = context as Context;
            var queueStatuses = new List<QueueStatus>();

            using (var client = new HttpClient())
            {
                var managementUri = new Uri(ctx.ManagementUri);

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(
                        System.Text.Encoding.ASCII.GetBytes(
                            managementUri.UserInfo
                        ))
                );

                using (var response = await client.GetAsync(new Uri(managementUri, "/api/queues")))
                {

                    var json = await response.Content.ReadAsStringAsync();

                    var queues = JsonConvert.DeserializeObject<IEnumerable<NativeQueue>>(json);

                    foreach (var queue in queues.Where(q => !q.Name.Contains("_")))
                    {
                        var failure = queues.FirstOrDefault(q => q.Name == queue.Name + "_failure");
                        var retry = queues.FirstOrDefault(q => q.Name == queue.Name + "_retry");

                        queueStatuses.Add(new QueueStatus
                        {
                            Name = queue.Name,
                            Connected = queue.Consumers,
                            Outstanding = queue.Messages_Unacknowledged,
                            Length = queue.Messages_Ready,
                            FailureLength = failure?.Messages_Ready ?? 0,
                            RetryLength = retry?.Messages_Ready ?? 0
                        });
                    }
                }
            }
            return queueStatuses;
        }

        Task<bool> IMessageQueue.CheckHealthAsync(object context)
        {
            var queueContext = context as Context;
            try
            {
                queueContext.TestConnection();
                return Task.FromResult(true);
            }
            catch (Exception) { }

            return Task.FromResult(true);

        }
    }
}
