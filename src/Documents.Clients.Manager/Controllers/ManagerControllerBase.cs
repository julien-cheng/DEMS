namespace Documents.Clients.Manager.Controllers
{
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Exceptions;
    using Documents.Common;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public abstract class ManagerControllerBase : Controller
    {
        private DateTime RequestStarted; // tracking action response time
        protected bool SuppressWrapper = false;

        protected ILogger Logger = null;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // for timing the request
            this.RequestStarted = DateTime.Now;

            this.Logger = Logging.CreateLogger(this.GetType());

            base.OnActionExecuting(context);
        }

        // we're going to change the response object type and wrap it with an object
        // that normalizes successful responses and exceptions.
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (SuppressWrapper)
            {
                base.OnActionExecuted(context);
                return;
            }

            // calculate the amount of time the action took to execute
            // this excludes any middleware overhead
            var elapsed = DateTime.Now.Subtract(this.RequestStarted).TotalMilliseconds;

            // check if the action threw an exception
            if (context.Exception != null)
            {
                this.Logger?.LogError(context.Exception, "Exception Manager Controller: {0}", context.Exception);

                // it threw, let's see if it's one of ours
                if (context.Exception is ExceptionBase)
                {
                    // this is one of our application exceptions, it contains a
                    // corresponding HTTP status code
                    var exception = context.Exception as ExceptionBase;

                    context.Result = new ObjectResult(new APIResponse<object>
                    {
                        Exception = exception.Message,
                        ExceptionType = exception.Type,
                        ExceptionStack = exception.StackTrace,
                        Success = false,
                        StatusCode = exception.StatusCode,
                        Elapsed = elapsed
                    });

                    context.HttpContext.Response.StatusCode = (int)exception.StatusCode;
                }
                else
                {
                    // this one was not thrown intentionally by our code
                    var exception = context.Exception;

                    context.Result = new ObjectResult(new APIResponse<object>
                    {
                        Exception = exception.Message,
                        ExceptionType = nameof(UnknownException),
                        ExceptionStack = exception.StackTrace,
                        Success = false,
                        StatusCode = HttpStatusCode.InternalServerError,
                        Elapsed = elapsed
                    });

                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                context.Exception = null;
            }
            else
            {
                // there was no exception
                // we will still wrap the result with a consistent decorator

                var objectResponse = context.Result is ObjectResult
                    ? (context.Result as ObjectResult).Value
                    : null;

                // this may end up legacy, but an action can return an APIResponse directly
                // without the need for wrapping

                if (objectResponse is APIResponse response)
                    context.HttpContext.Response.StatusCode = (int)response.StatusCode;
                else
                    // wrap the successful response
                    context.Result = new ObjectResult(new APIResponse<object>
                    {
                        Exception = null,
                        Success = true,
                        StatusCode = HttpStatusCode.OK,
                        Elapsed = elapsed,
                        Response = objectResponse
                    });
            }

            //base.OnActionExecuted(context);
        }

        protected async Task<APIResponse> APIExecuteAsync(Func<Task> execute)
        {
            return await APIExecuteAsync<bool>(async () =>
            {
                await execute();
                return true;
            });
        }

        protected async Task<APIResponse<T>> APIExecuteAsync<T>(Func<Task<T>> execute)
        {
            try
            {
                if (!ControllerContext.ModelState.IsValid)
                {
                    var state = ControllerContext.ModelState;

                    throw new Exception($"Request model is not valid");
                }

                var output = await execute();
                return new APIResponse<T>
                {
                    Exception = null,
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Response = output
                };

            }
            catch (ExceptionBase e)
            {
                return new APIResponse<T>
                {
                    Exception = e.Message,
                    ExceptionType = e.Type,
                    ExceptionStack = e.StackTrace,
                    Success = false,
                    StatusCode = e.StatusCode,
                    Response = default(T)
                };
            }
            catch (Exception e)
            {
                return new APIResponse<T>
                {
                    Exception = e.Message,
                    ExceptionType = nameof(UnknownException),
                    ExceptionStack = e.StackTrace,
                    Success = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    Response = default(T)
                };
            }
        }
    }
}

