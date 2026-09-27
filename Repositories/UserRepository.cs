using MongoDB.Driver;
using WardrobeApi.Models;
using WardrobeApi.Services;

namespace WardrobeApi.Repositories;

// The only class that touches raw MongoDB driver calls for User data.
// Controllers depend on IUserRepository instead, which Moq can mock
// directly (no "virtual" tricks needed - it's a plain interface).
public class UserRepository : IUserRepository
{
    private readonly MongoDbContext _db;

    public UserRepository(MongoDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByEmailAsync(string email) =>
        _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync()!;

    public Task<User?> GetByIdAsync(string id) =>
        _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync()!;

    public Task CreateAsync(User user) =>
        _db.Users.InsertOneAsync(user);

    public Task ConfirmEmailAsync(string userId)
    {
        var update = Builders<User>.Update
            .Set(u => u.EmailConfirmed, true)
            .Set(u => u.EmailConfirmationToken, (string?)null);

        return _db.Users.UpdateOneAsync(u => u.Id == userId, update);
    }

    public async Task<bool> EmailInUseByAnotherUserAsync(string email, string excludingUserId)
    {
        var existing = await _db.Users
            .Find(u => u.Email == email && u.Id != excludingUserId)
            .FirstOrDefaultAsync();

        return existing != null;
    }

    public Task UpdateProfileAsync(string userId, string? name, string? email)
    {
        var updates = new List<UpdateDefinition<User>>();

        if (!string.IsNullOrWhiteSpace(name))
            updates.Add(Builders<User>.Update.Set(u => u.Name, name));

        if (!string.IsNullOrWhiteSpace(email))
            updates.Add(Builders<User>.Update.Set(u => u.Email, email));

        if (updates.Count == 0)
            return Task.CompletedTask;

        var combined = Builders<User>.Update.Combine(updates);
        return _db.Users.UpdateOneAsync(u => u.Id == userId, combined);
    }

    public Task UpdatePasswordHashAsync(string userId, string newPasswordHash)
    {
        var update = Builders<User>.Update.Set(u => u.PasswordHash, newPasswordHash);
        return _db.Users.UpdateOneAsync(u => u.Id == userId, update);
    }

    public Task UpdateFaceShapeAsync(string userId, string shape)
    {
        var update = Builders<User>.Update.Set(u => u.FaceShape, shape);
        return _db.Users.UpdateOneAsync(u => u.Id == userId, update);
    }

    public Task UpdateBodyShapeAsync(string userId, string shape)
    {
        var update = Builders<User>.Update.Set(u => u.BodyShape, shape);
        return _db.Users.UpdateOneAsync(u => u.Id == userId, update);
    }

    public Task UpdateSkinUndertoneAsync(string userId, string undertone)
    {
        var update = Builders<User>.Update.Set(u => u.SkinUndertone, undertone);
        return _db.Users.UpdateOneAsync(u => u.Id == userId, update);
    }
}