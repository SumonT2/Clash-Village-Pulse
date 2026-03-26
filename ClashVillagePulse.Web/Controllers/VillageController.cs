using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashVillagePulse.Web.Controllers;

[Authorize]
public class VillageController : Controller
{
    private readonly IVillageQueryService _villageQueryService;
    private readonly IClashProfileSyncService _clashProfileSyncService;

    public VillageController(
        IVillageQueryService villageQueryService,
        IClashProfileSyncService clashProfileSyncService)
    {
        _villageQueryService = villageQueryService;
        _clashProfileSyncService = clashProfileSyncService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var villages = await _villageQueryService.GetMyVillagesAsync(userId, cancellationToken);
        return View(villages);
    }

    [HttpGet]
    public async Task<IActionResult> Clan(CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var villages = await _villageQueryService.GetClanVillagesAsync(userId, cancellationToken);
        return View(villages);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var village = await _villageQueryService.GetVillageDetailsAsync(userId, id, cancellationToken);
        if (village is null)
            return NotFound();

        return View(village);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncProfile(Guid id, CancellationToken cancellationToken)
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await _clashProfileSyncService.SyncVillageProfileAsync(id, userId, cancellationToken);
            TempData["SuccessMessage"] = "Player and clan info synced successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}