namespace Documents.Clients.PCMSBridge.Controllers
{
    using Documents.Clients.PCMSBridge.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;

    public class AuthHandoffController : Controller
    {
        private readonly ILogger<AuthHandoffController> Logger;

        public AuthHandoffController(
            ILogger<AuthHandoffController> logger
        )
        {
            Logger = logger;
        }

        public IActionResult Index(AuthHandoffModel model)
        {
            Response.ContentType = "text/html";

            return Content($"<html><body><form id = \"form\" action=\"{model.Redirect}\" method=\"post\">"
                + $"<input type=\"hidden\" name=\"token\" value=\"{model.Token}\" />"
                + "</form>"
                + "<script type=\"text/javascript\">"
                + "document.getElementById('form').submit();"
                + "</script></body></html>");
        }
    }
}