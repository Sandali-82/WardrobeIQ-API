namespace WardrobeApi.DTOs;

// Optional fields - PATCH semantics. Send only what you want to change:
// name only, email only, or both.
public record UpdateProfileRequest(string? Name, string? Email);

public record UpdateProfileResponse(string Name, string Email);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);