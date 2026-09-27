using WardrobeApi.Models;

namespace WardrobeApi.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(string id);
    Task CreateAsync(User user);

    // Marks the user's email confirmed and clears the one-time token -
    // the controller is responsible for validating the token first.
    Task ConfirmEmailAsync(string userId);

    // True if some OTHER user already has this email (used when changing
    // an email, where the current user obviously already "has" it).
    Task<bool> EmailInUseByAnotherUserAsync(string email, string excludingUserId);

    Task UpdateProfileAsync(string userId, string? name, string? email);
    Task UpdatePasswordHashAsync(string userId, string newPasswordHash);

    Task UpdateFaceShapeAsync(string userId, string shape);
    Task UpdateBodyShapeAsync(string userId, string shape);
    Task UpdateSkinUndertoneAsync(string userId, string undertone);
}