using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WardrobeApi.DTOs;
using WardrobeApi.Models;
using WardrobeApi.Services;

namespace WardrobeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WornLogController : ControllerBase
{
    private readonly MongoDbContext _db;

    public WornLogController(MongoDbContext db)
    {
        _db = db;
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
        var filterBuilder = Builders<WornLog>.Filter;
        var filter = filterBuilder.Eq(w => w.UserId, CurrentUserId);

        if (from.HasValue)
            filter &= filterBuilder.Gte(w => w.DateWorn, from.Value.Date);
        if (to.HasValue)
            filter &= filterBuilder.Lte(w => w.DateWorn, to.Value.Date);

        var logs = await _db.WornLogs.Find(filter).SortBy(w => w.DateWorn).ToListAsync();
        if (logs.Count == 0) return Ok(new List<WornLogResponse>());

        // One extra query to resolve outfit names, rather than N+1 lookups per log
        var outfitIds = logs.Select(l => l.OutfitId).Distinct().ToList();
        var outfits = await _db.Outfits
            .Find(o => outfitIds.Contains(o.Id))
            .ToListAsync();
        var outfitNameById = outfits.ToDictionary(o => o.Id, o => o.Name);

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
        var outfit = await _db.Outfits
            .Find(o => o.Id == request.OutfitId && o.UserId == CurrentUserId)
            .FirstOrDefaultAsync();

        if (outfit == null)
            return BadRequest(new { message = "Outfit not found in your account." });

        var dateOnly = request.DateWorn.Date;

        // One outfit-log per day: if the user already logged something for this
        // date, overwrite it instead of creating a duplicate entry.
        var existing = await _db.WornLogs
            .Find(w => w.UserId == CurrentUserId && w.DateWorn == dateOnly)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            var update = Builders<WornLog>.Update.Set(w => w.OutfitId, request.OutfitId);
            await _db.WornLogs.UpdateOneAsync(w => w.Id == existing.Id, update);
            return Ok(new WornLogResponse(existing.Id, request.OutfitId, outfit.Name, dateOnly));
        }

        var log = new WornLog
        {
            UserId = CurrentUserId,
            OutfitId = request.OutfitId,
            DateWorn = dateOnly
        };

        await _db.WornLogs.InsertOneAsync(log);
        return Ok(new WornLogResponse(log.Id, log.OutfitId, outfit.Name, log.DateWorn));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.WornLogs.DeleteOneAsync(
            w => w.Id == id && w.UserId == CurrentUserId);

        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }
}
