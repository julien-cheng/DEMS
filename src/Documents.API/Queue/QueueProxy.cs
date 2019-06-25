namespace Documents.API.Queue
{
    using Documents.API.Authentication;
    using Documents.API.Common;
    using Documents.API.Common.Websocket;
    using Documents.API.Common.Websocket.Messages;
    using Documents.Queues.Interfaces;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueProxy : IDisposable, IHttpContextAccessor
    {
        private readonly ILogger<QueueProxy> Logger;
        private readonly DocumentsAPIConfiguration DocumentsAPIConfiguration;
        private readonly JWT JWT;

        private SemaphoreSlim WebsocketSendingSemaphore = new SemaphoreSlim(1, 1);

        private ISecurityContext SecurityContext; // not DI... see use

        private const int PATROL_DELAYED_START_MS = 30000;
        private const int PATROL_INTERVAL_MS = 10000;

        private HttpContext HttpContext = null;
        HttpContext IHttpContextAccessor.HttpContext { get => HttpContext; set => HttpContext = value; }

        private WebSocket socket = null;
        private string queueName = null;
        private DateTime scheduledReconnect;
        private bool isProcessing = false;
        private ISubscription subscription = null;

        public QueueProxy(
            ILogger<QueueProxy> logger,
            DocumentsAPIConfiguration documentsAPIConfiguration,
            JWT jwt
        )
        {
            Logger = logger;
            DocumentsAPIConfiguration = documentsAPIConfiguration;
            this.JWT = jwt;
        }

        private async Task CloseAsync()
        {
            var source = new CancellationTokenSource();
            source.CancelAfter(1000);
            if (socket.State == WebSocketState.Open)
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closed", source.Token);
            socket.Abort();
        }

        private void Patrol(object state)
        {
            try
            {
                Task.Run(async () =>
                {
                    try

                    {
                        Logger.LogDebug($"QueueSubscription[{queueName}]: Patrol entry");
                        await SendLockAsync(async () =>
                        {
                            if (socket.State == WebSocketState.Open)
                            {
                                await JsonStreaming.Send(socket, new QueueControlMessage
                                {
                                    Type = QueueControlMessage.QueueControlMessageTypes.Ping,
                                    Message = new Common.Models.QueueMessage()
                                });
                                Logger.LogDebug($"QueueSubscription[{queueName}]: Ping Sent");
                            }

                            if (!isProcessing && DateTime.Now > scheduledReconnect)
                            {
                                // cancel the processing of any new messages
                                await subscription?.Disconnect();
                                await this.CloseAsync();
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Exception in patrol, ${exception}", e);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogError("Exception in patrol, ${exception}", e);
            }
        }

        private async Task SendLockAsync(Func<Task> action)
        {
            await WebsocketSendingSemaphore.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                WebsocketSendingSemaphore.Release();
            }
        }

        public async Task Connect(HttpContext httpContext, WebSocket webSocket, CancellationToken cancellationToken = default(CancellationToken))
        {

            socket = webSocket;

            using (var timer = new Timer(this.Patrol, null, PATROL_DELAYED_START_MS, PATROL_INTERVAL_MS))
            {

                HttpContext = httpContext; // set the global instance so that we can serve as an IHttpContextAccessor
                                           //this is messy DI-wise because of the HttpContext accessor not handling this scenario
                SecurityContext = new TokenSecurityContext(this, JWT);

                var configurationJSON = $"{{\"AMQPUri\": \"{DocumentsAPIConfiguration.QueueURI}\"}}";
                var driverTypeName = "Documents.Queues.RabbitMQ.Driver.MessageQueue, Documents.Queues.RabbitMQ.Driver";

                var context = QueueConnectionPool.GetContext(configurationJSON, driverTypeName);
                queueName = httpContext.Request.Query["queue"].ToArray().FirstOrDefault();
                if (queueName == null)
                    throw new System.Exception("Missing \"queue\" parameter");

                bool isCallback = httpContext.Request.Query["isCallback"].ToArray().Any(a => a?.ToLower() == "true");

                // schedule a reconnection roughly every ReconnectStaleSubscriptionSecondsAverage seconds
                var secondsUntilReconnect = DocumentsAPIConfiguration.ReconnectStaleSubscriptionSecondsAverage
                    * (new Random().NextDouble() + .5); // +/- 50% to avoid a rush of simultaneous reconnections

                Logger.LogInformation("Scheduling reconnection {0} seconds from now", secondsUntilReconnect);

                scheduledReconnect = DateTime.Now.AddSeconds(secondsUntilReconnect);

                Logger.LogInformation($"QueueSubscription[{queueName}]: {SecurityContext.UserIdentifier}");
                using (subscription = await QueueConnectionPool.Queue.Subscribe(context, queueName,
                    onMessage: async (message) =>
                    {
                        isProcessing = true;
                        Logger.LogInformation($"QueueSubscription[{queueName}]: new message {message.ID} {SecurityContext.UserIdentifier}");
                        await SendLockAsync(async () =>
                        {
                            await JsonStreaming.Send(webSocket, new QueueControlMessage
                            {
                                Type = QueueControlMessage.QueueControlMessageTypes.Incoming,
                                Message = new Common.Models.QueueMessage
                                {
                                    ID = message.ID,
                                    Message = message.Message,
                                    Retries = message.Retries,
                                    Callback = message.Callback
                                },
                                Queue = queueName
                            }, cancellationToken);
                        });

                    },
                    onError: async (error) =>
                    {
                        try
                        {
                            await CloseAsync();
                        }
                        catch (Exception)
                        { }
                    },
                    isCallback: isCallback))
                {
                    // this will be called every Patrol interval AND after every ack/nak.


                    await JsonStreaming.Send(webSocket, new QueueControlMessage
                    {
                        Type = QueueControlMessage.QueueControlMessageTypes.Connected,
                        Message = new Common.Models.QueueMessage()
                    });

                    var cleanClose = await JsonStreaming.Receive<QueueControlMessage>(webSocket, async m =>
                    {
                        IMessage message = new SimpleMessage
                        {
                            Message = m.Message?.Message,
                            ID = m.Message.ID,
                            Retries = m.Message.Retries,
                            Callback = m.Message.Callback
                        };

                        switch (m?.Type)
                        {
                            case QueueControlMessage.QueueControlMessageTypes.Ack:
                                Logger.LogDebug($"QueueSubscription[{queueName}]: ack {message.ID} {SecurityContext.UserIdentifier}");
                                await subscription.AckMessageAsync(message, true);
                                isProcessing = false;
                                break;
                            case QueueControlMessage.QueueControlMessageTypes.Nak:
                                Logger.LogInformation($"QueueSubscription[{queueName}]: nak {message.ID} {SecurityContext.UserIdentifier}");
                                await subscription.AckMessageAsync(message, false);
                                isProcessing = false;
                                break;
                            case QueueControlMessage.QueueControlMessageTypes.Ping:
                                //Console.WriteLine("Ping");
                                break;
                        }


                    }, cancellationToken);

                    await this.CloseAsync();

                    Logger.LogInformation($"QueueSubscription[{queueName}]: Closed {SecurityContext.UserIdentifier}");
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

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