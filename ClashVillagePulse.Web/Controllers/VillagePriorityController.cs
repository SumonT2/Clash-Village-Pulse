using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashVillagePulse.Web.Controllers;

[Authorize]
public class VillagePriorityController : Controller
{
    private readonly IVillageQueryService _villageQueryService;
    private readonly IPriorityService _priorityService;

    public VillagePriorityController(
        IVillageQueryService villageQueryService,
        IPriorityService priorityService)
    {
        _villageQueryService = villageQueryService;
        _priorityService = priorityService;
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid villageId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var model = await _villageQueryService.GetVillagePriorityEditAsync(userId, villageId, cancellationToken);
        if (model is null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        Guid villageId,
        [FromForm] List<SavePriorityItemDto> items,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await _priorityService.SaveVillagePrioritiesAsync(userId, villageId, items, cancellationToken);
            TempData["SuccessMessage"] = "Village priorities saved.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { villageId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuggestToVillage(
        Guid sourceVillageId,
        Guid targetVillageId,
        string? message,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var result = await _priorityService.SuggestVillagePriorityToVillageAsync(
                userId,
                sourceVillageId,
                targetVillageId,
                message,
                cancellationToken);

            TempData["SuccessMessage"] = result.AddedCount > 0
                ? $"Added {result.AddedCount} pending suggestion(s) to {result.TargetVillageName}. Skipped {result.MissingInTargetCount} missing item(s), {result.AlreadyPrioritizedCount} already prioritized, and {result.AlreadyPendingCount} already pending."
                : $"No new pending suggestions were added to {result.TargetVillageName}. Skipped {result.MissingInTargetCount} missing item(s), {result.AlreadyPrioritizedCount} already prioritized, and {result.AlreadyPendingCount} already pending.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { villageId = sourceVillageId });
    }
}
