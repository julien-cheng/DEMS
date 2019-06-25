namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Events;
    using Documents.Store;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class OrganizationController : ModelControllerBase<OrganizationModel, OrganizationIdentifier, IOrganizationStore>
    {
        protected override EventBase EventGet(OrganizationModel model) => new OrganizationGetEvent();
        protected override EventBase EventPut(OrganizationModel model) => new OrganizationPutEvent();
        protected override EventBase EventPost(OrganizationModel model) => new OrganizationPostEvent();
        protected override EventBase EventDelete(OrganizationIdentifier identifier) => new OrganizationDeleteEvent();

        protected EventBase EventGetAll() => new OrganizationGetEvent();

        public OrganizationController(IOrganizationStore store, IEventSender eventSender, ISecurityContext securityContext)
            : base(store, eventSender, securityContext)
        {
        }

        [HttpGet, Route("all")]
        public async Task<PagedResults<OrganizationModel>> GetAll(List<PopulationDirective> population)
        {
            var relationship = population.FirstOrDefault() ?? new PopulationDirective { };

            var models = await Store.LoadRelatedToAsync(null as object, relationship, population);

            if (models == null)
                ExceptionlessStatusCode = System.Net.HttpStatusCode.NotFound;

            await FireEvent(EventGetAll(), null);

            return models;
        }
    }
}
