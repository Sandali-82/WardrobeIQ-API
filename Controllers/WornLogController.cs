using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeApi.DTOs;
using WardrobeApi.Models;
using WardrobeApi.Repositories;

namespace WardrobeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WornLogController : ControllerBase
{
    private readonly IWornLogRepository _wornLogs;

    public WornLogController(IWornLogRepository wornLogs)
    {
        _wornLogs = wornLogs;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    // GET /api/wornlog?from=2026-08-01&to=2026-08-31
    // Powers the calendar view - fetch all logs in a month/date range at once
    // instead of one request per day.
    [HttpGet]
    public async Task<ActionResult<List<WornLogResponse>>> GetRange(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var logs = await _wornLogs.GetRangeForUserAsync(CurrentUserId, from, to);
        if (logs.Count == 0) return Ok(new List<WornLogResponse>());

        // One extra query to resolve outfit names, rather than N+1 lookups per log
        var outfitIds = logs.Select(l => l.OutfitId).Distinct().ToList();
        var outfitNameById = await _wornLogs.GetOutfitNamesByIdAsync(outfitIds);

        var response = logs.Select(l => new WornLogResponse(
            l.Id,
            l.OutfitId,
            outfitNameById.GetValueOrDefault(l.OutfitId, "(deleted outfit)"),
            l.DateWorn
        ));

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<WornLogResponse>> Create(CreateWornLogRequest request)
    {
        var outfit = await _wornLogs.GetOwnedOutfitAsync(request.OutfitId, CurrentUserId);
        if (outfit == null)
            return BadRequest(new { message = "Outfit not found in your account." });

        var dateOnly = request.DateWorn.Date;

        // One outfit-log per day: if the user already logged something for this
        // date, overwrite it instead of creating a duplicate entry.
        var existing = await _wornLogs.GetByUserAndDateAsync(CurrentUserId, dateOnly);

        if (existing != null)
        {
            await _wornLogs.UpdateOutfitForExistingLogAsync(existing.Id, request.OutfitId);
            return Ok(new WornLogResponse(existing.Id, request.OutfitId, outfit.Name, dateOnly));
        }

        var log = new WornLog
        {
            UserId = CurrentUserId,
            OutfitId = request.OutfitId,
            DateWorn = dateOnly
        };

        var created = await _wornLogs.CreateAsync(log);
        return Ok(new WornLogResponse(created.Id, created.OutfitId, outfit.Name, created.DateWorn));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _wornLogs.DeleteAsync(id, CurrentUserId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}