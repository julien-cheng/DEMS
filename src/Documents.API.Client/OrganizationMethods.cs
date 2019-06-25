namespace Documents.API.Client
{
    using Documents.API.Common.Models;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class OrganizationMethods : RESTBase<OrganizationModel, OrganizationIdentifier>
    {
        public OrganizationMethods(Connection connection)
            : base(connection, APIEndpoint.Organization)
        { }

        public Task<PagedResults<OrganizationModel>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken))
            => Connection.APICallAsync<PagedResults<OrganizationModel>>(HttpMethod.Get, APIEndpoint.OrganizationAll, cancellationToken: cancellationToken);

    }
}
