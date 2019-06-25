namespace Documents.API
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    public class StreamingResult : ActionResult
    {
        private Func<HttpResponse, Task> Content;

        public StreamingResult(Func<HttpResponse, Task> content)
        {
            this.Content = content ?? throw new ArgumentNullException("content");
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            await Content(context.HttpContext.Response);
        }
    }
}
