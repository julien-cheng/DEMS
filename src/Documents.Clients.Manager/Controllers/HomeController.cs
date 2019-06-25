namespace Documents.Clients.Manager.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using NSwag.Annotations;

    [SwaggerIgnore]
    public class HomeController : Controller
    {
        protected readonly ManagerConfiguration managerConfiguration;
        public HomeController(ManagerConfiguration managerConfiguration)
        {
            this.managerConfiguration = managerConfiguration;
        }

        public IActionResult Spa()
        {
            // We're going to make sure that this at least has a cookie.
            if (!Request.Cookies.ContainsKey("jwt.api") 
                && !Request.Path.Value.StartsWith("/ediscoverylanding/")
                && !Request.Path.Value.StartsWith("/leouploadlanding/")
            )
            {
                return new RedirectResult(string.Format(this.managerConfiguration.LoginRedirect, Request.Path));
            }
            else
            {
                return File("~/index.html", "text/html");
            }
        }
    }
}