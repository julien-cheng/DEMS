
namespace Documents.Queues.RabbitMQ.Driver
{
    // global says include at-the-root instead of 
    // searching within our namespace and seeing Documents.Queues.RabbitMQ.XXX
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;
    using Interfaces;
    using System;
    using System.Text;
    using System.Threading.Tasks;

    public class Subscription : ISubscription
    {
        public IModel Channel { get; set; }
        private string QueueName { get; set; }
        private Context Context { get; set; }

        private EventingBasicConsumer Consumer = null;
        private EventHandler<ConsumerEventArgs> ConsumerCallback = null;
        private EventHandler<BasicDeliverEventArgs> ReceivedCallback = null;

        public Subscription(Context context, IConnection connection, string queueName, bool isCallback = false)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("queueName argument is null or empty");
            if (connection == null)
                throw new ArgumentException("connection argument is null");

            Context = context;
            QueueName = queueName.ToLower();
            Channel = connection.CreateModel();
            Context.DefineQueue(Channel, queueName, isCallback);
            Channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        public void RegisterListener(Func<IMessage, Task> onMessage, Func<string, Task> onError)
        {
            Consumer = new EventingBasicConsumer(Channel);
            
            ConsumerCallback = async (model, ea) =>
            {
                try
                {
                    await onError(ea.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in RabbitMQ ConsumerCancelled handler: " + e);
                }
            };
            Consumer.ConsumerCancelled += ConsumerCallback;

            ReceivedCallback = async(model, ea) =>
            {

                int retries = 0;
                string callback = null;
                var headers = ea.BasicProperties.Headers;
                if (headers.ContainsKey("retries"))
                    retries = int.Parse(headers["retries"].ToString());
                if (headers.ContainsKey("callback"))
                    callback = Encoding.UTF8.GetString(headers["callback"] as byte[]);

                var queueMessage = new SimpleMessage
                {
                    ID = ea.DeliveryTag.ToString(),
                    Message = Encoding.UTF8.GetString(ea.Body, 0, ea.Body.Length),
                    Retries = retries,
                    Callback = callback
                };


                try
                {
                    await onMessage(queueMessage);
                }
                catch (Exception)
                {
                    // swallowing exception instead of throwing from event handler
                }
            };
            Consumer.Received += ReceivedCallback;

            Channel.BasicConsume(queue: QueueName, autoAck: false, consumer: Consumer);
        }

        Task ISubscription.AckMessageAsync(IMessage message, bool success)
        {
            var id = ulong.Parse(message.ID);

            if (success)
            {
                if (message.Callback != null)
                {
                    Context.PostCallback(message.Callback, message);
                }

                Channel.BasicAck(id, false);
            }
            else
            {
                // ack the original message, and republish to either the retry or failure exchanges
                string exchange = (message.Retries > 0)
                    ? Context.ExchangeRetry
                    : Context.ExchangeFailure;
                
                Context.PostToQueue(Channel, exchange, QueueName, message.Message, message.Retries -1);

                // Ack the original FAILED message because it's now in some other exchange
                Channel.BasicAck(id, true);
            }

            return Task.FromResult(0);
        }

        Task ISubscription.Disconnect()
        {
            try
            {
                Channel.Close();
            }
            catch (Exception) { }

            return Task.FromResult(0);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        Consumer.ConsumerCancelled -= ConsumerCallback;
                        Consumer.Received -= ReceivedCallback;

                        Channel.Close();
                        Channel.Dispose();
                    }
                    catch (Exception) { }
                }

                Context = null;
                Channel = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Subscription() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}