namespace Documents.API.Client
{
    using Documents.API.Common.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueMethods
    {
        private readonly Connection Connection;
        public QueueMethods(Connection connection)
        {
            this.Connection = connection;
        }

        public async Task<QueueSubscription> SubscribeAsync(string queueName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var subscription = new QueueSubscription(Connection);
            await subscription.Connect(queueName, cancellationToken);

            return subscription;
        }

        public async Task<bool> EnqueueAsync(string queueName, object message, string callback = null)
        {
            return await Connection.APICallAsync<bool>(HttpMethod.Post, APIEndpoint.Queue,
                queryStringContent: new
                {
                    queueName,
                    callback
                },
                bodyContent: message
            );
        }

        public async Task<bool> EnqueueAsync(string queueName, IEnumerable<object> messages)
        {
            return await Connection.APICallAsync<bool>(HttpMethod.Post, APIEndpoint.QueueBatch,
                bodyContent: messages
                    .Select(m => new QueuePair
                    {
                        QueueName = queueName,
                        Message = JsonConvert.SerializeObject(m)
                    })
            );
        }

        public async Task<bool> EnqueueAsync(IEnumerable<QueuePair> pairs)
        {
            return await Connection.APICallAsync<bool>(HttpMethod.Post, APIEndpoint.QueueBatch,
                bodyContent: pairs
            );
        }

        public async Task<QueueMessage> EnqueueWithCallbackWait(string queueName, object message, int waitTimeMS)
        {
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(waitTimeMS);
                return await EnqueueWithCallbackWait(queueName, message, cts.Token);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<QueueMessage> EnqueueWithCallbackWait(string queueName, object message, CancellationToken cancellationToken = default(CancellationToken))
        {
            var callbackMessage = null as QueueMessage;
            var callback = $"cb:{queueName}:{Guid.NewGuid().ToString()}";
            using (var subscription = await CreateCallbackAsync(callback, cancellationToken))
            {
                await subscription.ListenAsync(
                    onConnected: async () =>
                    {
                        await EnqueueAsync(queueName, message, callback: callback);
                    },
                    onMessage: async (m) =>
                    {
                        callbackMessage = m;

                        await subscription.Ack(m, true);
                        await subscription.Close();
                    },
                    cancellationToken: cancellationToken
                );
            }
            return callbackMessage;
        }

        public async Task<QueueSubscription> CreateCallbackAsync(string callback, CancellationToken cancellationToken = default(CancellationToken))
        {
            var subscription = new QueueSubscription(Connection, isCallback: true);

            try
            {
                await subscription.Connect(callback, cancellationToken);
            }
            catch(Exception)
            {
                try
                {
                    ((IDisposable)subscription).Dispose();
                }
                catch (Exception) { }

                throw;
            }

            return subscription;
        }

        public async Task<string> CreateCallbackAsync(CallbackModel callbackModel)
        {
            return await Connection.APICallAsync<string>(HttpMethod.Post, APIEndpoint.QueueCreateCallback,
                bodyContent: callbackModel
            );
        }

        public async Task<IEnumerable<QueueStatus>> GetStatus(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Connection.APICallAsync<IEnumerable<QueueStatus>>(HttpMethod.Get, APIEndpoint.QueueStatus);
        }
    }
}
