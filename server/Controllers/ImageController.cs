using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;

namespace InkyDesk.Server.Controllers;

public class ImageController(ILogger<ImageController> logger, IConfiguration config) : Controller
{
    public async Task<IActionResult> Index()
    {
        logger.LogDebug("Loading image...");

        var chromeExecPath = config.GetValue<string>("ChromeExecPath");

        if (string.IsNullOrWhiteSpace(chromeExecPath))
        {
            logger.LogDebug("Fetching browser...");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            logger.LogDebug("Fetching browser complete!");
        }

        logger.LogDebug("Launching browser...");
        var launchOptions = string.IsNullOrWhiteSpace(chromeExecPath)
            ? new LaunchOptions { Headless = true }
            : new LaunchOptions { Headless = true, ExecutablePath = chromeExecPath };
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
