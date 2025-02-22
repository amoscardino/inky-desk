using InkyDesk.Server.Models;
using InkyDesk.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace InkyDesk.Server.Controllers;

public class DisplayController(
    ILogger<DisplayController> logger,
    CalendarService calendarService
) : Controller
{
    public async Task<IActionResult> Index()
    {
        logger.LogDebug("Loading page...");

        var model = new DisplayModel
        {
            Events = await calendarService.GetEventsAsync()
        };

        return View(model);
    }
}
