using MongoDB.Driver;
using WardrobeApi.Models;
using WardrobeApi.Services;

namespace WardrobeApi.Repositories;

public class ClothingItemRepository : IClothingItemRepository
{
    private readonly MongoDbContext _db;

    public ClothingItemRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<ClothingItem>> GetAllForUserAsync(string userId, string? category, string? color)
    {
        var filterBuilder = Builders<ClothingItem>.Filter;
        var filter = filterBuilder.Eq(i => i.UserId, userId);

        if (!string.IsNullOrWhiteSpace(category))
            filter &= filterBuilder.Eq(i => i.Category, category);

        if (!string.IsNullOrWhiteSpace(color))
            filter &= filterBuilder.Eq(i => i.Color, color);

        return await _db.ClothingItems.Find(filter).SortByDescending(i => i.CreatedAt).ToListAsync();
    }

    public Task<ClothingItem?> GetByIdAsync(string id, string userId) =>
        _db.ClothingItems.Find(i => i.Id == id && i.UserId == userId).FirstOrDefaultAsync()!;

    public Task CreateAsync(ClothingItem item) =>
        _db.ClothingItems.InsertOneAsync(item);

    public async Task<bool> UpdateAsync(string id, string userId, ClothingItem updatedFields)
    {
        var update = Builders<ClothingItem>.Update
            .Set(i => i.Name, updatedFields.Name)
            .Set(i => i.ImageUrl, updatedFields.ImageUrl)
            .Set(i => i.Category, updatedFields.Category)
            .Set(i => i.Color, updatedFields.Color)
            .Set(i => i.Season, updatedFields.Season)
            .Set(i => i.Tags, updatedFields.Tags);

        var result = await _db.ClothingItems.UpdateOneAsync(
            i => i.Id == id && i.UserId == userId, update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _db.ClothingItems.DeleteOneAsync(i => i.Id == id && i.UserId == userId);
        return result.DeletedCount > 0;
    }
}
