namespace Documents.Backends.Gateway.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Route("healthcheck")]
    public class HealthCheckController
    {
        [HttpGet]
        public string Get()
        {
            return "OK";
        }
    }
}
