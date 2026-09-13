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
public class OutfitController : ControllerBase
{
    private readonly MongoDbContext _db;

    public OutfitController(MongoDbContext db)
    {
        _db = db;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    [HttpGet]
    public async Task<ActionResult<List<OutfitResponse>>> GetAll()
    {
        var outfits = await _db.Outfits
            .Find(o => o.UserId == CurrentUserId)
            .SortByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(outfits.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OutfitResponse>> GetById(string id)
    {
        var outfit = await _db.Outfits
            .Find(o => o.Id == id && o.UserId == CurrentUserId)
            .FirstOrDefaultAsync();

        if (outfit == null) return NotFound();
        return Ok(ToResponse(outfit));
    }

    [HttpPost]
    public async Task<ActionResult<OutfitResponse>> Create(CreateOutfitRequest request)
    {
        if (request.ItemIds == null || request.ItemIds.Count == 0)
            return BadRequest(new { message = "An outfit needs at least one clothing item." });

        // Make sure every referenced item actually belongs to this user -
        // otherwise someone could save an outfit pointing at another user's items.
        var ownedCount = await _db.ClothingItems.CountDocumentsAsync(
            i => i.UserId == CurrentUserId && request.ItemIds.Contains(i.Id));

        if (ownedCount != request.ItemIds.Count)
            return BadRequest(new { message = "One or more items don't exist in your wardrobe." });

        var outfit = new Outfit
        {
            UserId = CurrentUserId,
            Name = request.Name,
            ItemIds = request.ItemIds
        };

        await _db.Outfits.InsertOneAsync(outfit);
        return CreatedAtAction(nameof(GetById), new { id = outfit.Id }, ToResponse(outfit));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, CreateOutfitRequest request)
    {
        var ownedCount = await _db.ClothingItems.CountDocumentsAsync(
            i => i.UserId == CurrentUserId && request.ItemIds.Contains(i.Id));

        if (ownedCount != request.ItemIds.Count)
            return BadRequest(new { message = "One or more items don't exist in your wardrobe." });

        var update = Builders<Outfit>.Update
            .Set(o => o.Name, request.Name)
            .Set(o => o.ItemIds, request.ItemIds);

        var result = await _db.Outfits.UpdateOneAsync(
            o => o.Id == id && o.UserId == CurrentUserId, update);

        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Outfits.DeleteOneAsync(
            o => o.Id == id && o.UserId == CurrentUserId);

        if (result.DeletedCount == 0) return NotFound();

        // Clean up any worn-log entries pointing at the deleted outfit so the
        // calendar doesn't end up with dangling references.
        await _db.WornLogs.DeleteManyAsync(w => w.OutfitId == id && w.UserId == CurrentUserId);

        return NoContent();
    }

    private static OutfitResponse ToResponse(Outfit o) =>
        new(o.Id, o.Name, o.ItemIds, o.CreatedAt);
}
