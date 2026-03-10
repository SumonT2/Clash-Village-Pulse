using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClashVillagePulse.Web.Controllers;

[Authorize]
public class StaticDataController : Controller
{
    private readonly IStaticDataGenerationService _service;

    public StaticDataController(IStaticDataGenerationService service)
    {
        _service = service;
    }

    public IActionResult Generate()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Generate(StaticDataGenerationRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

        var runId = await _service.StartGenerationAsync(userId, request);

        return RedirectToAction(nameof(Progress), new { id = runId });
    }

    public IActionResult Progress(Guid id)
    {
        ViewBag.RunId = id;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRunStatus(Guid id)
    {
        var run = await _service.GetRunAsync(id);

        if (run == null)
            return NotFound();

        return Json(run);
    }
}