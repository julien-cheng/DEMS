namespace Documents.Clients.Manager.Models.Responses
{
    using Documents.API.Common.Models;
    using System.Collections.Generic;

    public class BatchResponse
    {
        public IEnumerable<APIResponse> OperationResponses { get; set; }
    }
}
