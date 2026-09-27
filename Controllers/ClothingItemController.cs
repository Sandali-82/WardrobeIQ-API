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
public class ClothingItemController : ControllerBase
{
    private readonly IClothingItemRepository _items;

    public ClothingItemController(IClothingItemRepository items)
    {
        _items = items;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    [HttpGet]
    public async Task<ActionResult<List<ClothingItemResponse>>> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? color)
    {
        var items = await _items.GetAllForUserAsync(CurrentUserId, category, color);
        return Ok(items.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClothingItemResponse>> GetById(string id)
    {
        var item = await _items.GetByIdAsync(id, CurrentUserId);
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

        await _items.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, CreateClothingItemRequest request)
    {
        var updatedFields = new ClothingItem
        {
            Name = request.Name,
            ImageUrl = request.ImageUrl,
            Category = request.Category,
            Color = request.Color.ToLowerInvariant(),
            Season = request.Season,
            Tags = request.Tags ?? new List<string>()
        };

        var updated = await _items.UpdateAsync(id, CurrentUserId, updatedFields);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _items.DeleteAsync(id, CurrentUserId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private static ClothingItemResponse ToResponse(ClothingItem i) =>
        new(i.Id, i.Name, i.ImageUrl, i.Category, i.Color, i.Season, i.Tags, i.CreatedAt);
}