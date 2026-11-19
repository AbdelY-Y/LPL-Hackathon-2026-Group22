using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using harbor.Models;

namespace harbor.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment environment;

    public HomeController(IWebHostEnvironment environment) => this.environment = environment;

    public IActionResult Index()
    {
        var profiles = new List<FinancialProfile>();
        var run = TempData.Peek("RedactionRun") as string;
        if (run != null && Guid.TryParseExact(run, "N", out _))
        {
            var path = Path.Combine(environment.ContentRootPath, "data", "redaction", run, "results.json");
            if (System.IO.File.Exists(path))
                profiles.Add(new FinancialProfile { Id = 4,
                    Name = TempData.Peek("ProfileName") as string ?? "",
                    ProfileType = TempData.Peek("ProfileType") as string ?? "Individual",
                    RedactedDocuments = JsonSerializer.Deserialize<RedactionResponse>(System.IO.File.ReadAllText(path))!.Outputs });
        }
        return View(profiles);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
