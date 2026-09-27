using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeApi.DTOs;
using WardrobeApi.Models;
using WardrobeApi.Repositories;
using WardrobeApi.Services;

namespace WardrobeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IJwtService _jwt;
    private readonly IEmailService _emailService;

    public AuthController(IUserRepository users, IJwtService jwt, IEmailService emailService)
    {
        _users = users;
        _jwt = jwt;
        _emailService = emailService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existing = await _users.GetByEmailAsync(request.Email);
        if (existing != null)
            return Conflict(new { message = "An account with this email already exists." });

        var confirmationToken = Guid.NewGuid().ToString("N");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmailConfirmed = false,
            EmailConfirmationToken = confirmationToken
        };

        await _users.CreateAsync(user);

        // Built from the actual request host, so it works whether the
        // backend is reached over the phone hotspot's local IP (dev) or
        // the deployed domain (production) - no hardcoded config needed.
        var confirmLink = $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email?userId={user.Id}&token={confirmationToken}";

        try
        {
            await _emailService.SendConfirmationEmailAsync(user.Email, confirmLink);
        }
        catch (Exception ex)
        {
            // Don't fail registration just because the email couldn't be
            // sent (e.g. SMTP hiccup) - the account still exists and the
            // user can be resent a confirmation link later if needed.
            // But DO log it, otherwise a real SMTP problem goes silent.
            Console.WriteLine($"[EMAIL SEND FAILED] {ex.GetType().Name}: {ex.Message}");
        }

        // No JWT returned here - the user only gets a token once they've
        // confirmed their email, via the confirm-email endpoint below.
        return Ok(new { message = "Registration successful. Please check your email to confirm your account." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null || user.EmailConfirmationToken != token)
        {
            return Content(BuildResultHtml(
                success: false,
                heading: "Link invalid or expired",
                message: "This confirmation link is no longer valid. Please try registering again."
            ), "text/html");
        }

        await _users.ConfirmEmailAsync(userId);

        // This endpoint is hit directly by the phone's browser (a real
        // https link, so it's reliably clickable from Gmail) - not by the
        // app itself. So instead of returning JSON, it returns a small
        // HTML page that immediately tries to hand off to the app via the
        // wardrobeiq:// custom scheme, carrying a freshly generated JWT so
        // the app can log the user straight in. A manual button is the
        // fallback in case the automatic redirect is blocked.
        var jwtToken = _jwt.GenerateToken(user);
        var appLink = $"wardrobeiq://login-success?token={Uri.EscapeDataString(jwtToken)}" +
                      $"&name={Uri.EscapeDataString(user.Name)}&email={Uri.EscapeDataString(user.Email)}";

        return Content(BuildResultHtml(
            success: true,
            heading: "Email confirmed!",
            message: "Redirecting you to WardrobeIQ...",
            appLink: appLink
        ), "text/html");
    }

    private static string BuildResultHtml(bool success, string heading, string message, string? appLink = null)
    {
        var redirectScript = appLink != null
            ? $"<script>window.location.href = '{appLink}';</script>"
            : "";

        var button = appLink != null
            ? $"<a href='{appLink}' style='display:inline-block;margin-top:20px;padding:12px 24px;" +
              "background:#D4AF6A;color:#1A1418;text-decoration:none;border-radius:8px;font-weight:bold;" +
              $"font-family:sans-serif;'>Open WardrobeIQ</a>"
            : "";

        var color = success ? "#D4AF6A" : "#C4645B";

        return $@"
            <html>
            <head><meta name='viewport' content='width=device-width, initial-scale=1'></head>
            <body style='font-family: sans-serif; background:#1A1418; color:#F5EFE8;
                         display:flex; flex-direction:column; align-items:center; justify-content:center;
                         height:100vh; margin:0; text-align:center; padding:20px;'>
                <h2 style='color:{color};'>{heading}</h2>
                <p>{message}</p>
                {button}
                {redirectScript}
            </body>
            </html>";
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!user.EmailConfirmed)
            return Unauthorized(new { message = "Please confirm your email before logging in." });

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

        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            // Only check for a conflict if the email is actually changing -
            // this is a genuine email-change operation on the same account,
            // not creating a new one; we just need to keep emails unique.
            var inUse = await _users.EmailInUseByAnotherUserAsync(request.Email, CurrentUserId);
            if (inUse)
                return Conflict(new { message = "That email is already in use by another account." });
        }

        await _users.UpdateProfileAsync(CurrentUserId, request.Name, request.Email);

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

        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _users.UpdatePasswordHashAsync(CurrentUserId, newHash);

        return NoContent();
    }
}