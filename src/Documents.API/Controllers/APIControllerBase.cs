namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Exceptions;
    using Documents.API.Common.Models;
    using Documents.API.Exceptions;
    using Documents.Common;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;
    using Serilog.Context;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    [
        Authorize,
        Route("api/v1/[controller]")
    ]
    public abstract class APIControllerBase : Controller
    {
        private DateTime RequestStarted; // tracking action response time
        protected bool SuppressWrapper = false;
        protected HttpStatusCode ExceptionlessStatusCode = HttpStatusCode.OK;
        protected ILogger Logger = null;

        private List<IDisposable> LogProperties = new List<IDisposable>();
        private readonly ISecurityContext SecurityContext;

        public APIControllerBase(ISecurityContext securityContext)
        {
            this.SecurityContext = securityContext;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // for timing the request
            this.RequestStarted = DateTime.Now;

            this.Logger = Logging.CreateLogger(this.GetType());

            if (SecurityContext.IsAuthenticated)
            {
                LogProperties.Add(LogContext.PushProperty("initiatorOrganizationKey", SecurityContext.UserIdentifier.OrganizationKey));
                LogProperties.Add(LogContext.PushProperty("initiatorUserKey", SecurityContext.UserIdentifier.UserKey));
                LogProperties.Add(LogContext.PushProperty("initiatorUserAgent", Request.Headers["User-Agent"].FirstOrDefault()));
            }
            
            base.OnActionExecuting(context);
        }

        // we're going to change the response object type and wrap it with an object
        // that normalizes successful responses and exceptions.
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // calculate the amount of time the action took to execute
            // this excludes any middleware overhead
            var elapsed = DateTime.Now.Subtract(this.RequestStarted).TotalMilliseconds;

            if (SuppressWrapper)
            {
                base.OnActionExecuted(context);
                return;
            }

            // check if the action threw an exception
            if (context.Exception != null)
            {

                this.Logger?.LogError(context.Exception, "Exception in API Controller: {0}", context.Exception);

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
                else if (context.Exception is DocumentsException)
                {
                    var exception = context.Exception as DocumentsException;

                    context.Result = new ObjectResult(new APIResponse<object>
                    {
                        Exception = exception.Message,
                        ExceptionType = exception.GetType().Name,
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
                {
                    // wrap the successful response
                    context.Result = new ObjectResult(new APIResponse<object>
                    {
                        Exception = null,
                        Success = true,
                        StatusCode = ExceptionlessStatusCode,
                        Elapsed = elapsed,
                        Response = objectResponse
                    });

                    context.HttpContext.Response.StatusCode = (int)ExceptionlessStatusCode;
                }
            }

            foreach (var property in LogProperties)
                property.Dispose();
            LogProperties.Clear();

            base.OnActionExecuted(context);
        }
    }
}
