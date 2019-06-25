namespace Documents.API.Queue
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Net.WebSockets;
    using System.Threading.Tasks;

    public class QueueHandlerMiddleware
    {
        private readonly RequestDelegate Next;
        private readonly IServiceProvider ServiceProvider;

        public QueueHandlerMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            Next = next;
            ServiceProvider = serviceProvider;
        }

        public async Task Invoke(HttpContext context)
        {

            if (context.Request.Path == "/api/v1/queue/connect")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using (var scope = ServiceProvider.CreateScope())
                    using (var queueProxy = scope.ServiceProvider.GetService(typeof(QueueProxy)) as QueueProxy)
                    using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                        await queueProxy.Connect(context, webSocket);

                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await Next(context);
            }
        }
    }
}