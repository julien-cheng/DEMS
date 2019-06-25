namespace Documents.Clients.Manager.Controllers
{
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.Clients.Manager.Models.Responses;
    using Documents.Clients.Manager.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/path")]
    public class PathController : ControllerBaseCRUDService<ManagerPathModel, PathIdentifier, PathService>
    {
        public PathController(PathService pathService)
        {
            Service = pathService;
        }

        [HttpPost("moveinto")]
        public Task MoveInto(
            MoveIntoRequest request,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => Service.MoveIntoAsync(
            request.TargetPathIdentifier,
            request.SourcePathIdentifier,
            request.SourceFileIdentifier,
            cancellationToken: cancellationToken
        );

        [HttpGet("suggest")]
        public Task<IEnumerable<ManagerPathModel>> SuggestAsync(
            PathIdentifier identifier,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => Service.SuggestAsync(
            identifier,
            cancellationToken
        );

        [HttpPut("child")]
        public Task<ManagerPathModel> CreateChildAsync(
            [FromBody]
            NewPathRequest newPathRequest,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => Service.CreateChildAsync(
            newPathRequest.PathIdentifier,
            newPathRequest.Name,
            cancellationToken
        );

        [HttpGet("itemquery")]
        public Task<ItemQueryResponse> ItemQueryAsync(
            ItemQueryRequest request,
            bool isEdiscoveryUser,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => Service.ItemQueryAsync(
            request.PathIdentifier,
            isEdiscoveryUser,
            request.PageIndex,
            request.PageSize,
            request.SortField,
            request.SortAscending,
            cancellationToken: cancellationToken
        );
    }
}