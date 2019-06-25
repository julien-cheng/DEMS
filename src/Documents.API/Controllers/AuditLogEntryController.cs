namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Events;
    using Documents.Store;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AuditLogEntryController : ModelControllerBase<AuditLogEntryModel, AuditLogEntryIdentifier, IAuditLogEntryStore>
    {
        protected override EventBase EventGet(AuditLogEntryModel model) => new AuditLogEntryGetEvent();
        protected override EventBase EventPost(AuditLogEntryModel model) => new AuditLogEntryPostEvent();

        public AuditLogEntryController(IAuditLogEntryStore store, IEventSender eventSender, ISecurityContext securityContext)
            : base(store, eventSender, securityContext)
        {
        }

        protected override IEnumerable<PopulationDirective> DefaultPopulationRelationships => new[]
        {
            new PopulationDirective
            {
                Name = null,
                Paging = new PagingArguments()
            }
        };

        [HttpGet("all")]
        public async Task<PagedResults<AuditLogEntryModel>> Get(List<PopulationDirective> population)
        {
            var model = await Store.LoadRelatedToAsync(null as FileIdentifier,
                population.FirstOrDefault(p => p.Name == null), population ?? DefaultPopulationRelationships);

            return model;
        }

        [HttpPut]
        public override Task<AuditLogEntryModel> Put([FromBody]AuditLogEntryModel model)
            => throw new NotImplementedException();

        [HttpDelete]
        public override Task<AuditLogEntryIdentifier> Delete(AuditLogEntryIdentifier identifier)
            => throw new NotImplementedException();

    }
}