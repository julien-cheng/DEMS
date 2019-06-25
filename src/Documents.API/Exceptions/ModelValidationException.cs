namespace Documents.API.Exceptions
{
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public class ModelValidationException : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

        public IEnumerable<string> Errors { get; set; }
        public ModelValidationException(ModelStateDictionary modelState) : base("Request Model is invalid")
        {
            Errors = modelState
                .SelectMany(ms => ms.Value.Errors)
                .Select(e => e.ErrorMessage)
                .ToArray();
        }
    }
}
