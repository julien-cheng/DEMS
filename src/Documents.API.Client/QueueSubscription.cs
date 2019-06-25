namespace Documents.API.Client
{
    using Documents.API.Common.Models;
    using Documents.API.Common.Websocket;
    using Documents.API.Common.Websocket.Messages;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueSubscription : IDisposable
    {
        private readonly Connection Connection;
        private readonly Timer PingTimer;
        private readonly bool IsCallback = false;

        private ClientWebSocket WebSocket;
        public string QueueName { get; private set; } = null;
        private const int PING_TIMER_FREQUENCY = 10000;

        private SemaphoreSlim WebsocketSendingSemaphore = new SemaphoreSlim(1, 1);

        private DateTime? LastPing = null;
        private const int PING_TIMEOUT_THRESHOLD = 45000;

        private bool IsExecuting = false;

        public QueueSubscription(Connection connection, bool isCallback = false)
        {
            this.Connection = connection;
            this.PingTimer = new Timer(this.Ping, null, 1000, PING_TIMER_FREQUENCY);
            this.IsCallback = isCallback;
            this.LastPing = DateTime.Now;
        }
        
        public void Ping(object stateInfo)
        {
            if (WebSocket != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (WebSocket.State == WebSocketState.Open)
                        {
                            await WebsocketSendingSemaphore.WaitAsync();
                            try
                            {
                                await JsonStreaming.Send(WebSocket, new QueueControlMessage
                                {
                                    Type = QueueControlMessage.QueueControlMessageTypes.Ping,
                                    Queue = QueueName,
                                    Message = new QueueMessage()
                                });
                            }
                            finally
                            {
                                WebsocketSendingSemaphore.Release();
                            }
                        }

                        if (!this.IsExecuting
                            && this.LastPing != null
                            && DateTime.Now.Subtract(this.LastPing.Value).TotalMilliseconds > PING_TIMEOUT_THRESHOLD
                        )
                        {
                            Connection.Logger.LogWarning("Ping timeout, closing");
                            //Console.WriteLine("Ping timeout, closing");
                            await this.Close();
                        }
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine("Exception in ping handler: " + e);
                    }
                });
            }
        }

        public async Task Connect(
            string queue, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            this.QueueName = queue;
            this.LastPing = DateTime.Now;
            
            WebSocket = await Connection.ConnectWebSocket(APIEndpoint.QueueConnect,
                querystring: new { queue, IsCallback },
                cancellationToken: cancellationToken
            );
        }

        public async Task ListenAsync(
            Func<QueueMessage, Task> onMessage,
            Func<Task> onConnected = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            await JsonStreaming.Receive<QueueControlMessage>(WebSocket, async m =>
            {
                switch (m?.Type)
                {
                    case QueueControlMessage.QueueControlMessageTypes.Connected:
                        if (onConnected != null)
                            await onConnected();
                        break;
                    case QueueControlMessage.QueueControlMessageTypes.Incoming:
                        IsExecuting = true;

                        await onMessage(m.Message);

                        this.LastPing = DateTime.Now;
                        IsExecuting = false;
                        break;
                    case QueueControlMessage.QueueControlMessageTypes.Ping:
                        Connection.Logger.LogDebug("Ping Received");
                        if (DateTime.Now.Subtract(this.LastPing.Value).TotalMilliseconds > PING_TIMEOUT_THRESHOLD)
                            Connection.Logger.LogDebug("Ping Received, late. socket thread is blocked.");

                        this.LastPing = DateTime.Now;
                        break;
                }

            }, cancellationToken);

            //Console.WriteLine("Receive has returned");
        }

        public async Task Close(CancellationToken cancellationToken = default(CancellationToken))
        {
            var source = new CancellationTokenSource();
            source.CancelAfter(1000);
            if (WebSocket.State == WebSocketState.Open)
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closed", source.Token);
            WebSocket.Abort();
        }

        public async Task Ack(QueueMessage message, bool success)
        {
            await WebsocketSendingSemaphore.WaitAsync();
            try
            {
                await JsonStreaming.Send(WebSocket, new QueueControlMessage
                {
                    Type = success
                    ? QueueControlMessage.QueueControlMessageTypes.Ack
                    : QueueControlMessage.QueueControlMessageTypes.Nak,
                    Message = message,
                    Queue = QueueName
                });
            }
            finally
            {
                WebsocketSendingSemaphore.Release();
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
                    if (WebSocket != null)

                        try { WebSocket.Dispose(); } catch (Exception) { }
                    if (PingTimer != null)
                        try { PingTimer.Dispose(); } catch (Exception) { }
                }

                disposedValue = true;
            }
        }
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
