namespace Documents.Clients.Manager.Controllers
{
    using Documents.Clients.Manager.Common;
    using Microsoft.AspNetCore.Mvc;
    using NSwag.Annotations;
    using System.Threading.Tasks;

    [SwaggerIgnore]
    public class HealthCheckController
    {
        private readonly APIConnection APIConnection;
        public HealthCheckController(APIConnection api)
        {
            this.APIConnection = api;
        }

        [HttpGet]
        public async Task<bool> Index()
        {
            if (!(await APIConnection.HealthGetAsync()))
                throw new System.Exception("API is unhealthy");

            return true;
        }
    }
}
