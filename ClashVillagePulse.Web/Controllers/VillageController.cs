using System.Security.Claims;
using ClashVillagePulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClashVillagePulse.Web.Controllers;

[Authorize]
public class VillageController : Controller
{
    private readonly IVillageQueryService _villageQueryService;

    public VillageController(IVillageQueryService villageQueryService)
    {
        _villageQueryService = villageQueryService;
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
}