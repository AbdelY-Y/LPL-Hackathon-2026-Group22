using harbor.Models;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace harbor.Controllers;

public class ProfileController : Controller
{
    private readonly IWebHostEnvironment environment;

    public ProfileController(IWebHostEnvironment environment)
    {
        this.environment = environment;
    }

    [HttpGet]
    public IActionResult Create() => View(new FinancialProfile());

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Create(FinancialProfile profile, List<IFormFile> documents)
    {
        if (documents.Count == 0 || documents.Count > 10)
            ModelState.AddModelError("", "Choose between 1 and 10 PDFs.");
        if (documents.Any(file => file.Length == 0 || file.Length > 10 * 1024 * 1024 ||
            Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf"))
            ModelState.AddModelError("", "Use PDFs up to 10 MB each.");
        if (!ModelState.IsValid) return View(profile);

        var run = Guid.NewGuid().ToString("N");
        var folder = Path.Combine(environment.ContentRootPath, "data", "redaction", run);
        Directory.CreateDirectory(folder);
        var inputs = new List<string>();
        try
        {
            foreach (var file in documents)
            {
                // Originals are temporary and never served by the web application.
                var input = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".pdf");
                inputs.Add(input);
                await using (var stream = System.IO.File.Create(input))
                    await file.CopyToAsync(stream);
                var signature = new byte[5];
                await using (var stream = System.IO.File.OpenRead(input))
                    await stream.ReadExactlyAsync(signature);
                if (System.Text.Encoding.ASCII.GetString(signature) != "%PDF-")
                    throw new InvalidDataException();
            }

            var start = new ProcessStartInfo
            {
                FileName = Path.Combine(environment.ContentRootPath, ".venv", "bin", "python"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            start.ArgumentList.Add(Path.Combine(environment.ContentRootPath, "backend", "LocalRedact.py"));
            foreach (var input in inputs) start.ArgumentList.Add(input);
            start.ArgumentList.Add("--client-id");
            start.ArgumentList.Add(run);
            start.ArgumentList.Add("--output-dir");
            start.ArgumentList.Add(folder);
            start.ArgumentList.Add("--no-ner");

            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(true); throw; }
            var details = await error;
            if (process.ExitCode != 0)
            {
                var message = "Document processing failed. Check the PDFs and S3 connection.";
                if (details.Contains("ExpiredToken"))
                    message = "AWS credentials expired. Refresh them in the terminal and restart Harbor.";
                else if (details.Contains("Unable to locate credentials") || details.Contains("Partial credentials"))
                    message = "AWS credentials are missing. Set them in the terminal running Harbor, then restart it.";
                else if (details.Contains("AccessDenied"))
                    message = "AWS denied the upload. Check permission to upload to the team’s S3 bucket.";
                else if (details.Contains("InvalidAccessKeyId") || details.Contains("SignatureDoesNotMatch"))
                    message = "AWS credentials were rejected. Check the access key, secret key and session token.";
                ModelState.AddModelError("", message);
                return View(profile);
            }
            var result = JsonSerializer.Deserialize<RedactionResponse>(await output)!;
            if (result.Outputs.Count != documents.Count) throw new InvalidDataException();
            for (var index = 0; index < result.Outputs.Count; index++)
                result.Outputs[index].InputFile = Path.GetFileName(documents[index].FileName);
            await System.IO.File.WriteAllTextAsync(Path.Combine(folder, "results.json"), JsonSerializer.Serialize(result));
            TempData["RedactionRun"] = run;
        }
        catch
        {
            ModelState.AddModelError("", "Could not complete document processing. Check the PDFs, AWS credentials and S3 access, then try again.");
            return View(profile);
        }
        finally
        {
            foreach (var input in inputs) System.IO.File.Delete(input);
        }

        TempData["ProfileName"] = profile.Name.Trim();
        TempData["ProfileType"] = profile.ProfileType;
        return RedirectToAction(nameof(Details), new { id = 4 });
    }

    [HttpGet]
    public IActionResult Download(string run, string file)
    {
        // The current demo session can only download its own redacted output.
        if (run != TempData.Peek("RedactionRun") as string || !Guid.TryParseExact(run, "N", out _) ||
            Path.GetFileName(file) != file || Path.GetExtension(file) != ".pdf") return NotFound();
        var folder = Path.Combine(environment.ContentRootPath, "data", "redaction", run);
        var result = JsonSerializer.Deserialize<RedactionResponse>(System.IO.File.ReadAllText(Path.Combine(folder, "results.json")))!;
        var document = result.Outputs.FirstOrDefault(item => item.OutputFile == file);
        if (document == null) return NotFound();
        return PhysicalFile(Path.Combine(folder, file), "application/pdf", Path.GetFileNameWithoutExtension(document.InputFile) + "_redacted.pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(string run)
    {
        if (run != TempData.Peek("RedactionRun") as string || !Guid.TryParseExact(run, "N", out _))
            return NotFound();
        var folder = Path.Combine(environment.ContentRootPath, "data", "redaction", run);
        // Local files stay recoverable; the S3 copy is not deleted.
        if (Directory.Exists(folder))
        {
            var deleted = Path.Combine(environment.ContentRootPath, "data", "deleted");
            Directory.CreateDirectory(deleted);
            Directory.Move(folder, Path.Combine(deleted, run));
        }
        TempData.Remove("RedactionRun");
        TempData.Remove("ProfileName");
        TempData.Remove("ProfileType");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (id == 4)
        {
            var profile = new FinancialProfile
            {
                Id = 4,
                Name = TempData.Peek("ProfileName") as string ?? "",
                ProfileType = TempData.Peek("ProfileType") as string ?? "Individual"
            };
            var run = TempData.Peek("RedactionRun") as string;
            if (run != null && Guid.TryParseExact(run, "N", out _))
            {
                var results = Path.Combine(environment.ContentRootPath, "data", "redaction", run, "results.json");
                if (System.IO.File.Exists(results))
                {
                    profile.RedactionRun = run;
                    var redaction = JsonSerializer.Deserialize<RedactionResponse>(System.IO.File.ReadAllText(results))!;
                    profile.RedactedDocuments = redaction.Outputs;
                    profile.UploadHeld = redaction.UploadHeld;
                }
            }
            var demoFile = Path.Combine(environment.ContentRootPath, "sample-data", "household-demo.json");
            if (System.IO.File.Exists(demoFile))
                profile.Demo = JsonSerializer.Deserialize<HouseholdDemo>(System.IO.File.ReadAllText(demoFile),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            // Optional shared results feed; the URL stays on the server, outside source control.
            var resultsUrl = Environment.GetEnvironmentVariable("HARBOR_RESULTS_URL");
            if (!string.IsNullOrWhiteSpace(resultsUrl) && !profile.UploadHeld)
            {
                try
                {
                    var response = await HouseholdResults.Load(resultsUrl);
                    if (response.TryGetProperty("financial_profile", out _))
                    {
                        profile.ServerResults = ServerFinancialResults.Parse(response);
                        profile.Demo = null;
                        profile.ResultsNotice = profile.ServerResults.ClientId == profile.RedactionRun
                            ? "S3 results · Matched to this upload"
                            : "S3 results · From another upload";
                    }
                    else
                    {
                        profile.Demo = HouseholdResults.Parse(response);
                        profile.ResultsNotice = "Shared household results · Not matched to this upload";
                    }
                }
                catch (HttpRequestException error)
                {
                    profile.ResultsNotice = error.StatusCode.HasValue
                        ? $"S3 returned HTTP {(int)error.StatusCode.Value}. Check the signed link and file access. Showing local data."
                        : "Could not connect to S3. Showing local data.";
                }
                catch (TaskCanceledException)
                {
                    profile.ResultsNotice = "S3 took too long to respond. Showing local data.";
                }
                catch (Exception error) when (error is InvalidDataException or UriFormatException)
                {
                    profile.ResultsNotice = "Use the full HTTPS signed link from the team’s S3 bucket. Showing local data.";
                }
                catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
                {
                    profile.ResultsNotice = "The server JSON does not match Harbor’s expected fields. Showing local data.";
                }
                catch
                {
                    profile.ResultsNotice = "Server results unavailable. Showing the local fictional summary.";
                }
            }
            return profile.RedactedDocuments.Count == 0 ? NotFound() : View(profile);
        }
        return NotFound();
    }
}
