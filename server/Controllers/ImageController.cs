using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;

namespace InkyDesk.Server.Controllers;

public class ImageController(ILogger<ImageController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        logger.LogDebug("Loading image...");

        logger.LogDebug("Fetching browser...");
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        logger.LogDebug("Fetching browser complete!");

        logger.LogDebug("Launching browser...");
        var launchOptions = new LaunchOptions { Headless = true };
        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        logger.LogDebug("Launching browser complete!");

        logger.LogDebug("Loading page...");
        await using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 400, Height = 300 });
        await page.GoToAsync(Url.Action("Index", "Display", null, Request.Scheme));
        logger.LogDebug("Loading page complete!");

        logger.LogDebug("Getting screenshot...");
        var image = await page.ScreenshotDataAsync();
        logger.LogDebug("Getting screenshot complete!");

        return File(image, "image/png");
    }
}
