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
public class AuthController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(MongoDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var existing = await _db.Users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        if (existing != null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _db.Users.InsertOneAsync(user);

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Name, user.Email));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Name, user.Email));
    }

    // ---------- Profile edit ----------

    // PATCH /api/auth/profile - partial update: send only the field(s) you
    // want to change. Name-only, email-only, or both are all valid.
    [Authorize]
    [HttpPatch("profile")]
    public async Task<ActionResult<UpdateProfileResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Provide at least a name or email to update." });

        var user = await _db.Users.Find(u => u.Id == CurrentUserId).FirstOrDefaultAsync();
        if (user == null) return NotFound();

        var updates = new List<UpdateDefinition<User>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
            updates.Add(Builders<User>.Update.Set(u => u.Name, request.Name));

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            // Only check for a conflict if the email is actually changing -
            // this is a genuine email-change operation on the same account,
            // not creating a new one; we just need to keep emails unique.
            var existing = await _db.Users
                .Find(u => u.Email == request.Email && u.Id != CurrentUserId)
                .FirstOrDefaultAsync();

            if (existing != null)
                return Conflict(new { message = "That email is already in use by another account." });

            updates.Add(Builders<User>.Update.Set(u => u.Email, request.Email));
        }

        if (updates.Count > 0)
        {
            var combined = Builders<User>.Update.Combine(updates);
            await _db.Users.UpdateOneAsync(u => u.Id == CurrentUserId, combined);
        }

        return Ok(new UpdateProfileResponse(
            request.Name ?? user.Name,
            request.Email ?? user.Email
        ));
    }

    // PUT /api/auth/change-password - full replace (both fields required),
    // PUT stays semantically correct here since there's nothing partial
    // about a password change.
    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters." });

        var user = await _db.Users.Find(u => u.Id == CurrentUserId).FirstOrDefaultAsync();
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        var update = Builders<User>.Update.Set(u => u.PasswordHash, newHash);
        await _db.Users.UpdateOneAsync(u => u.Id == CurrentUserId, update);

        return NoContent();
    }
}
