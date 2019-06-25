namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Queue;
    using Documents.Store;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    public class HealthController : APIControllerBase
    {
        private readonly IHealthStore HealthStore;
        private readonly IQueueSender QueueSender;

        public HealthController(IHealthStore healthStore, ISecurityContext securityContext, IQueueSender queueSender)
             : base(securityContext)
        {
            this.HealthStore = healthStore;
            this.QueueSender = queueSender;
        }

        [HttpGet, AllowAnonymous]
        public async Task<bool> Get()
        {

            if (!(await HealthStore.CheckHealthAsync()))
                throw new System.Exception("Database is unhealthy");

            if (!(await QueueSender.CheckHealthAsync()))
                throw new System.Exception("Queue is unhealthy");

            //if (!(await BackendClient.CheckHealthAsync()))
            //  throw new System.Exception("Gateway is unhealthy");


            return true;
        }
    }
}
