namespace Documents.Clients.Manager.Common
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    class StreamActionResult : IActionResult
    {
        private readonly Func<HttpResponse, Task> _writeToResponseFunc;

        public StreamActionResult(Func<HttpResponse, Task> writeToResponseFunc)
        {
            _writeToResponseFunc = writeToResponseFunc ?? throw new ArgumentNullException(nameof(writeToResponseFunc));
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            return _writeToResponseFunc(context.HttpContext.Response);
        }
    }
}
