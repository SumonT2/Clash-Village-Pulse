using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashVillagePulse.Web.Controllers;

[Authorize]
public class PrioritySuggestionController : Controller
{
    private readonly IPriorityService _priorityService;

    public PrioritySuggestionController(IPriorityService priorityService)
    {
        _priorityService = priorityService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SubmitPrioritySuggestionDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await _priorityService.SubmitSuggestionAsync(userId, request, cancellationToken);
            TempData["SuccessMessage"] = "Suggestion submitted.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Details", "Village", new { id = request.VillageId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(
        Guid suggestionId,
        Guid villageId,
        bool accept,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await _priorityService.RespondToSuggestionAsync(userId, suggestionId, accept, cancellationToken);
            TempData["SuccessMessage"] = accept ? "Suggestion accepted." : "Suggestion rejected.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Details", "Village", new { id = villageId });
    }
}