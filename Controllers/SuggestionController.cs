using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeApi.DTOs;
using WardrobeApi.Repositories;
using WardrobeApi.Services;

namespace WardrobeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuggestionController : ControllerBase
{
    private readonly IClothingItemRepository _items;
    private readonly IUserRepository _users;
    private readonly IGeminiService _gemini;

    public SuggestionController(IClothingItemRepository items, IUserRepository users, IGeminiService gemini)
    {
        _items = items;
        _users = users;
        _gemini = gemini;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    // GET /api/suggestion/occasion-types - lets the Flutter app fetch the
    // allowed values instead of hardcoding them client-side.
    [AllowAnonymous]
    [HttpGet("occasion-types")]
    public ActionResult<string[]> GetOccasionTypes() => Ok(OccasionTypes.Allowed);

    // POST /api/suggestion/occasion
    // Body: { "occasion": "casual", "notes": "outdoor, evening" }
    [HttpPost("occasion")]
    public async Task<ActionResult<OutfitSuggestionResponse>> SuggestForOccasion(OccasionSuggestionRequest request)
    {
        var occasion = request.Occasion?.Trim().ToLowerInvariant() ?? "";

        if (!OccasionTypes.Allowed.Contains(occasion))
        {
            return BadRequest(new
            {
                message = $"Invalid occasion. Allowed values: {string.Join(", ", OccasionTypes.Allowed)}"
            });
        }

        var wardrobe = await _items.GetAllForUserAsync(CurrentUserId, category: null, color: null);

        if (wardrobe.Count == 0)
            return BadRequest(new { message = "Add some clothing items to your wardrobe first." });

        var user = await _users.GetByIdAsync(CurrentUserId);

        try
        {
            var (itemIds, explanation) = await _gemini.SuggestOutfitAsync(
                wardrobe, occasion, request.Notes,
                user?.FaceShape, user?.BodyShape, user?.SkinUndertone);

            var validIds = wardrobe.Select(i => i.Id).ToHashSet();
            var filteredIds = itemIds.Where(validIds.Contains).ToList();

            return Ok(new OutfitSuggestionResponse(filteredIds, explanation));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = "Couldn't get a suggestion right now.", detail = ex.Message });
        }
    }
}