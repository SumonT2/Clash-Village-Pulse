using System.Security.Claims;
using System.Text;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClashVillagePulse.Web.Controllers;

[Authorize]
public class VillageUploadController : Controller
{
    private readonly IVillageUploadService _villageUploadService;

    public VillageUploadController(IVillageUploadService villageUploadService)
    {
        _villageUploadService = villageUploadService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new VillageUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        VillageUploadViewModel model,
        CancellationToken cancellationToken)
    {
        string? json = model.JsonText;

        if ((string.IsNullOrWhiteSpace(json)) && model.JsonFile is null)
        {
            ModelState.AddModelError(string.Empty, "Paste JSON or choose a JSON file.");
            return View(model);
        }

        if (model.JsonFile is not null)
        {
            using var reader = new StreamReader(model.JsonFile.OpenReadStream(), Encoding.UTF8);
            json = await reader.ReadToEndAsync(cancellationToken);
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var result = await _villageUploadService.UploadAsync(userId, json!, cancellationToken);

            TempData["SuccessMessage"] =
                $"Village '{result.VillageName}' ({result.PlayerTag}) uploaded successfully. " +
                $"Imported {result.TotalItemsImported} item rows.";

            return RedirectToAction("Details", "Village", new { id = result.VillageId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Upload failed: {ex.Message}");
            return View(model);
        }
    }
}