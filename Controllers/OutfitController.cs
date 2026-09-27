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
public class OutfitController : ControllerBase
{
    private readonly IOutfitRepository _outfits;

    public OutfitController(IOutfitRepository outfits)
    {
        _outfits = outfits;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    [HttpGet]
    public async Task<ActionResult<List<OutfitResponse>>> GetAll()
    {
        var outfits = await _outfits.GetAllForUserAsync(CurrentUserId);
        return Ok(outfits.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OutfitResponse>> GetById(string id)
    {
        var outfit = await _outfits.GetByIdAsync(id, CurrentUserId);
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
        var ownedCount = await _outfits.CountOwnedItemsAsync(CurrentUserId, request.ItemIds);
        if (ownedCount != request.ItemIds.Count)
            return BadRequest(new { message = "One or more items don't exist in your wardrobe." });

        var outfit = new Outfit
        {
            UserId = CurrentUserId,
            Name = request.Name,
            ItemIds = request.ItemIds
        };

        await _outfits.CreateAsync(outfit);
        return CreatedAtAction(nameof(GetById), new { id = outfit.Id }, ToResponse(outfit));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, CreateOutfitRequest request)
    {
        var ownedCount = await _outfits.CountOwnedItemsAsync(CurrentUserId, request.ItemIds);
        if (ownedCount != request.ItemIds.Count)
            return BadRequest(new { message = "One or more items don't exist in your wardrobe." });

        var updated = await _outfits.UpdateAsync(id, CurrentUserId, request.Name, request.ItemIds);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _outfits.DeleteAsync(id, CurrentUserId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private static OutfitResponse ToResponse(Outfit o) =>
        new(o.Id, o.Name, o.ItemIds, o.CreatedAt);
}