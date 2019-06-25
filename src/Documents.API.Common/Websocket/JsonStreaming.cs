namespace Documents.API.Common.Websocket
{
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class JsonStreaming
    {

        public static async Task<bool> Receive<T>(WebSocket webSocket, Func<T, Task> onReceive, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result = new WebSocketReceiveResult(0, WebSocketMessageType.Binary, false);

                while (!result.CloseStatus.HasValue && webSocket.State == WebSocketState.Open )
                {
                    int maxlength = 1024 * 1024 * 1;
                    using (var ms = new MemoryStream())
                    {
                        while (!result.EndOfMessage && !webSocket.CloseStatus.HasValue && ms.Length < maxlength)
                        {
                            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                            ms.Write(buffer, 0, result.Count);
                        }

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", cancellationToken);
                        }

                        if (!result.CloseStatus.HasValue)
                        {
                            if (result.EndOfMessage)
                            {
                                ms.Seek(0, SeekOrigin.Begin);

                                using (var reader = new StreamReader(ms, Encoding.UTF8))
                                {
                                    var json = await reader.ReadToEndAsync();
                                    var message = JsonConvert.DeserializeObject<T>(json);

                                    await onReceive(message);
                                }

                                result = new WebSocketReceiveResult(0, WebSocketMessageType.Binary, false);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task Send<T>(WebSocket webSocket, T message, CancellationToken cancellationToken = default(CancellationToken))
        {
            var jsonBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            await webSocket.SendAsync(new ArraySegment<byte>(jsonBuffer, 0, jsonBuffer.Length), WebSocketMessageType.Binary, true, cancellationToken);
        }
    }
}
