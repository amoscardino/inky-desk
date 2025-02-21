using Microsoft.AspNetCore.Mvc;

namespace InkyDesk.Server.Controllers;

public class DisplayController(ILogger<DisplayController> logger) : Controller
{
    public IActionResult Index()
    {
        logger.LogDebug("Loading page...");
        return View();
    }
}
