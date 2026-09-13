using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WardrobeApi.DTOs;
using WardrobeApi.Services;

namespace WardrobeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuggestionController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly GeminiService _gemini;

    public SuggestionController(MongoDbContext db, GeminiService gemini)
    {
        _db = db;
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

        var wardrobe = await _db.ClothingItems
            .Find(i => i.UserId == CurrentUserId)
            .ToListAsync();

        if (wardrobe.Count == 0)
            return BadRequest(new { message = "Add some clothing items to your wardrobe first." });

        var user = await _db.Users.Find(u => u.Id == CurrentUserId).FirstOrDefaultAsync();

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
