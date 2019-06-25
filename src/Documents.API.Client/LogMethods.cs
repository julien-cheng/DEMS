namespace Documents.API.Client
{
    using Documents.API.Common.Models;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class LogMethods : RESTBase<AuditLogEntryModel, AuditLogEntryIdentifier>
    {
        public LogMethods(Connection connection)
            : base(connection, APIEndpoint.AuditLog)
        { }

        public async Task<PagedResults<AuditLogEntryModel>> LoadAsync(
            IEnumerable<PopulationDirective> population = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return await Connection.APICallAsync<PagedResults<AuditLogEntryModel>>(
                HttpMethod.Get,
                APIEndpoint.AuditLogQuery,
                queryStringContent: new { population },
                cancellationToken: cancellationToken
            );
        }

        public override Task<AuditLogEntryModel> DeleteAsync(AuditLogEntryIdentifier identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override Task<AuditLogEntryModel> PutAsync(AuditLogEntryModel model, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
