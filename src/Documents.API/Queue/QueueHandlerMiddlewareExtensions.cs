namespace Documents.API.Queue
{
    using Microsoft.AspNetCore.Builder;

    public static class QueueHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseQueueHandler(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<QueueHandlerMiddleware>();
        }
    }
}