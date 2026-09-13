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
public class ClothingItemController : ControllerBase
{
    private readonly MongoDbContext _db;

    public ClothingItemController(MongoDbContext db)
    {
        _db = db;
    }

    // Pulled from the JWT "sub" claim set in JwtService - every endpoint
    // here only ever touches the logged-in user's own items.
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    [HttpGet]
    public async Task<ActionResult<List<ClothingItemResponse>>> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? color)
    {
        var filterBuilder = Builders<ClothingItem>.Filter;
        var filter = filterBuilder.Eq(i => i.UserId, CurrentUserId);

        if (!string.IsNullOrWhiteSpace(category))
            filter &= filterBuilder.Eq(i => i.Category, category);

        if (!string.IsNullOrWhiteSpace(color))
            filter &= filterBuilder.Eq(i => i.Color, color);

        var items = await _db.ClothingItems.Find(filter).SortByDescending(i => i.CreatedAt).ToListAsync();
        return Ok(items.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClothingItemResponse>> GetById(string id)
    {
        var item = await _db.ClothingItems
            .Find(i => i.Id == id && i.UserId == CurrentUserId)
            .FirstOrDefaultAsync();

        if (item == null) return NotFound();
        return Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ClothingItemResponse>> Create(CreateClothingItemRequest request)
    {
        var item = new ClothingItem
        {
            UserId = CurrentUserId,
            Name = request.Name,
            ImageUrl = request.ImageUrl,
            Category = request.Category,
            Color = request.Color.ToLowerInvariant(),
            Season = string.IsNullOrWhiteSpace(request.Season) ? "all-season" : request.Season,
            Tags = request.Tags ?? new List<string>()
        };

        await _db.ClothingItems.InsertOneAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, CreateClothingItemRequest request)
    {
        var update = Builders<ClothingItem>.Update
            .Set(i => i.Name, request.Name)
            .Set(i => i.ImageUrl, request.ImageUrl)
            .Set(i => i.Category, request.Category)
            .Set(i => i.Color, request.Color.ToLowerInvariant())
            .Set(i => i.Season, request.Season)
            .Set(i => i.Tags, request.Tags ?? new List<string>());

        var result = await _db.ClothingItems.UpdateOneAsync(
            i => i.Id == id && i.UserId == CurrentUserId, update);

        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.ClothingItems.DeleteOneAsync(
            i => i.Id == id && i.UserId == CurrentUserId);

        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }

    private static ClothingItemResponse ToResponse(ClothingItem i) =>
        new(i.Id, i.Name, i.ImageUrl, i.Category, i.Color, i.Season, i.Tags, i.CreatedAt);
}
