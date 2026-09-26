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
public class ProfileController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IGeminiService _gemini;

    public ProfileController(MongoDbContext db, IGeminiService gemini)
    {
        _db = db;
        _gemini = gemini;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    // ---------- Face shape ----------

    [HttpPost("face-shape")]
    public async Task<ActionResult<FaceShapeResponse>> CalculateFaceShape(FaceShapeRequest request)
    {
        if (request.ForeheadWidth <= 0 || request.CheekboneWidth <= 0 ||
            request.JawlineWidth <= 0 || request.FaceLength <= 0)
        {
            return BadRequest(new { message = "All measurements must be greater than zero." });
        }

        var (shape, explanation) = FaceShapeCalculator.Classify(
            request.ForeheadWidth, request.CheekboneWidth, request.JawlineWidth, request.FaceLength);

        var update = Builders<Models.User>.Update.Set(u => u.FaceShape, shape);
        await _db.Users.UpdateOneAsync(u => u.Id == CurrentUserId, update);

        return Ok(new FaceShapeResponse(shape, explanation));
    }

    [HttpGet("face-shape")]
    public async Task<ActionResult<object>> GetFaceShape()
    {
        var user = await _db.Users.Find(u => u.Id == CurrentUserId).FirstOrDefaultAsync();
        if (user?.FaceShape == null)
            return NotFound(new { message = "No face shape calculated yet." });

        return Ok(new { faceShape = user.FaceShape });
    }

    // ---------- Body shape ----------

    [HttpPost("body-shape")]
    public async Task<ActionResult<BodyShapeResponse>> CalculateBodyShape(BodyShapeRequest request)
    {
        if (request.ShoulderWidth <= 0 || request.BustWidth <= 0 ||
            request.WaistWidth <= 0 || request.HipWidth <= 0)
        {
            return BadRequest(new { message = "All measurements must be greater than zero." });
        }

        var (shape, explanation) = BodyShapeCalculator.Classify(
            request.ShoulderWidth, request.BustWidth, request.WaistWidth, request.HipWidth);

        var update = Builders<Models.User>.Update.Set(u => u.BodyShape, shape);
        await _db.Users.UpdateOneAsync(u => u.Id == CurrentUserId, update);

        return Ok(new BodyShapeResponse(shape, explanation));
    }

    [HttpGet("body-shape")]
    public async Task<ActionResult<object>> GetBodyShape()
    {
        var user = await _db.Users.Find(u => u.Id == CurrentUserId).FirstOrDefaultAsync();
        if (user?.BodyShape == null)
            return NotFound(new { message = "No body shape calculated yet." });

        return Ok(new { bodyShape = user.BodyShape });
    }

    // ---------- Skin undertone (photo-based, via Gemini vision) ----------

    [HttpPost("undertone")]
    public async Task<ActionResult<UndertoneResponse>> AnalyzeUndertone(UndertoneRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest(new { message = "An image is required." });

        try
        {
            var (undertone, explanation) = await _gemini.AnalyzeSkinUndertoneAsync(
                request.ImageBase64, request.MimeType);

            var update = Builders<Models.User>.Update.Set(u => u.SkinUndertone, undertone);
            await _db.Users.UpdateOneAsync(u => u.Id == CurrentUserId, update);

            return Ok(new UndertoneResponse(undertone, explanation));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = "Couldn't analyze the photo right now.", detail = ex.Message });
        }
    }

    [HttpGet("undertone")]
    public async Task<ActionResult<object>> GetUndertone()
    {
        var user = await _db.Users.Find(u => u.Id == CurrentUserId).FirstOrDefaultAsync();
        if (user?.SkinUndertone == null)
            return NotFound(new { message = "No skin undertone analyzed yet." });

        return Ok(new { skinUndertone = user.SkinUndertone });
    }

    // ---------- Styling guide (combines all three, occasion-independent) ----------

    [HttpGet("styling-guide")]
    public async Task<ActionResult<StylingGuideResponse>> GetStylingGuide()
    {
        var user = await _db.Users.Find(u => u.Id == CurrentUserId).FirstOrDefaultAsync();

        if (user?.FaceShape == null && user?.BodyShape == null && user?.SkinUndertone == null)
        {
            return BadRequest(new
            {
                message = "Calculate your face shape, body shape, or skin undertone first to get a styling guide."
            });
        }

        try
        {
            var guide = await _gemini.GetStylingGuideAsync(user.FaceShape, user.BodyShape, user.SkinUndertone);
            return Ok(new StylingGuideResponse(
                guide.Necklines, guide.Hairstyles, guide.Sleeves, guide.Silhouettes, guide.Colors, guide.Avoid));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = "Couldn't build a styling guide right now.", detail = ex.Message });
        }
    }
}