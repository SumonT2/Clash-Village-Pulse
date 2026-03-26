using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashVillagePulse.Web.Controllers;

[Authorize]
public class ClanPriorityController : Controller
{
    private readonly IVillageQueryService _villageQueryService;
    private readonly IPriorityService _priorityService;

    public ClanPriorityController(
        IVillageQueryService villageQueryService,
        IPriorityService priorityService)
    {
        _villageQueryService = villageQueryService;
        _priorityService = priorityService;
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        Guid villageId,
        VillageSection section,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var model = await _villageQueryService.GetClanPriorityTemplateAsync(userId, villageId, section, cancellationToken);
        if (model is null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        Guid villageId,
        VillageSection section,
        int hallLevel,
        [FromForm] List<SavePriorityItemDto> items,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await _priorityService.SaveClanPrioritiesAsync(userId, villageId, section, hallLevel, items, cancellationToken);
            TempData["SuccessMessage"] = "Clan priority template saved.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { villageId, section });
    }
}