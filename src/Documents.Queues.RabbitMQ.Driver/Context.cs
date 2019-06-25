using RabbitMQ.Client; // I'm up here because I overlap in default search path

// Reference for the library: https://www.rabbitmq.com/dotnet-api-guide.html
namespace Documents.Queues.RabbitMQ.Driver
{
    using Interfaces;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class Context : IDisposable
    {
        public string AMQPUri { get; set; }
        public string ManagementUri { get; set; } = "http://guest:guest@localhost:15672";
        public string ExchangeCallback = "CallbackExchange";
        public string ExchangeWork = "WorkExchange";
        public string ExchangeRetry = "RetryExchange";
        public string ExchangeFailure = "FailureExchange";

        public int RetryDelay = 5000;
        public int CallbackTimeout = 10000;
        public int RetriesBeforeFailure = 1;

        private volatile IConnection connection;

        private volatile List<string> QueuesDefined = new List<string>();

        public Context(string json)
        {
            try
            {
                JsonConvert.PopulateObject(json, this);
            }
            catch (Exception)
            {
                throw new Exception("Cannot deserialize json configuration for RabbitMQ driver");
            }

            Connect();
        }

        private void Connect()
        {
            if (connection != null)
                try
                {
                    connection.Dispose();
                }
                catch (Exception) { }

            var factory = new ConnectionFactory()
            {
                AutomaticRecoveryEnabled = false,
                TopologyRecoveryEnabled = false,
                Uri = new Uri(this.AMQPUri)
            };
            connection = factory.CreateConnection();
        }

        public void TestConnection()
        {
            if (connection == null || !connection.IsOpen)
                Connect();

            if (connection == null || !connection.IsOpen)
                throw new Exception("Cannot enqueue message, connection is not ready");

        }

        public Task Enqueue(string queue, IMessage message)
        {
            return Enqueue(queue, new IMessage[] { message });
        }

        public Task PostCallback(string callback, IMessage message)
        {
            using (var channel = connection.CreateModel())
            {
                IBasicProperties props = channel.CreateBasicProperties();
                props.Headers = new Dictionary<string, object>
                {
                    { "retries", 0 },
                };
                channel.BasicReturn += Channel_BasicReturn;
                channel.BasicPublish(
                    exchange: ExchangeCallback,
                    routingKey: callback.ToLower(),
                    basicProperties: props,
                    mandatory: true,
                    body: Encoding.UTF8.GetBytes(message.Message)
                );
            }

            return Task.FromResult(0);
        }

        private void Channel_BasicReturn(object sender, global::RabbitMQ.Client.Events.BasicReturnEventArgs e)
        {
            // logger?
        }

        public Task Enqueue(string queue, IEnumerable<IMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(queue))
                throw new ArgumentException("queue argument is null or empty");
            if (messages == null)
                throw new ArgumentException("message argument is null");
            TestConnection();
            queue = queue.ToLower();


            using (var channel = connection.CreateModel())
            {
                DefineQueue(channel, queue);
                foreach (var message in messages)
                    PostToQueue(channel, ExchangeWork, queue, message.Message, RetriesBeforeFailure, message.Callback);
            }
            return Task.FromResult(0);
        }

        public void PostToQueue(IModel channel, string exchange, string queue, string message, int retries, string callback = null)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>
                {
                    { "retries", retries },
                };

            if (callback != null)
                props.Headers.Add("callback", Encoding.UTF8.GetBytes(callback));

            channel.BasicPublish(
                exchange: exchange,
                routingKey: queue,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(message)
            );
        }

        public void DefineQueue(IModel channel, string queue, bool isCallback = false)
        {
            if (isCallback)
            {

                DefineExchangeQueueBinding(channel, queue, queue, ExchangeCallback,
                exclusive: true,
                autodelete: true);
            }
            else if (!QueuesDefined.Contains(queue))
            {
                lock (QueuesDefined)
                {
                    if (!QueuesDefined.Contains(queue)) // still?
                    {
                        // "Work" queue is the entry point
                        DefineExchangeQueueBinding(channel, queue, queue, ExchangeWork);

                        // "Retry" queue is for failed messages that aren't yet out of retries
                        // it holds a posted message for "RetryDelay" milliseconds, then posts it back to the work queue
                        DefineExchangeQueueBinding(channel, queue + "_retry", queue, ExchangeRetry, new Dictionary<string, object>()
                        {
                            { "x-dead-letter-exchange", ExchangeWork },
                            { "x-message-ttl", RetryDelay }
                        });

                        // "Failure" queue is for messages out of retries
                        DefineExchangeQueueBinding(channel, queue + "_failure", queue, ExchangeFailure);

                        QueuesDefined.Add(queue);
                    }
                }
            }
        }

        private void DefineExchangeQueueBinding(IModel channel, string queue, string baseQueue, string exchange, Dictionary<string, object> properties = null, bool exclusive = false, bool autodelete = false)
        {
            // declare a "Direct" exchange
            channel.ExchangeDeclare(
                exchange: exchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false
            );

            // declare the queue
            channel.QueueDeclare(
                queue: queue,
                durable: true,
                exclusive: exclusive,
                autoDelete: autodelete,
                arguments: properties
            );

            // bind the queue to the exchange
            channel.QueueBind(queue, exchange, baseQueue, null);
        }

        public Task<ISubscription> Subscribe(string queue,
            Func<IMessage, Task> onMessage,
            Func<string, Task> onError,
            bool isCallback = false)
        {
            if (string.IsNullOrWhiteSpace(queue))
                throw new ArgumentException("queue argument is null or empty");
            if (onMessage == null)
                throw new ArgumentException("onMessage argument is null");
            TestConnection();
            queue = queue.ToLower();

            var subscription = new Subscription(this, connection, queue, isCallback);
            subscription.RegisterListener(onMessage, onError);

            return Task.FromResult(subscription as ISubscription);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    connection?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Context() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}