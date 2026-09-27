using MongoDB.Driver;
using WardrobeApi.Models;
using WardrobeApi.Services;

namespace WardrobeApi.Repositories;

public class OutfitRepository : IOutfitRepository
{
    private readonly MongoDbContext _db;

    public OutfitRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<Outfit>> GetAllForUserAsync(string userId) =>
        await _db.Outfits.Find(o => o.UserId == userId).SortByDescending(o => o.CreatedAt).ToListAsync();

    public Task<Outfit?> GetByIdAsync(string id, string userId) =>
        _db.Outfits.Find(o => o.Id == id && o.UserId == userId).FirstOrDefaultAsync()!;

    public Task CreateAsync(Outfit outfit) =>
        _db.Outfits.InsertOneAsync(outfit);

    public async Task<bool> UpdateAsync(string id, string userId, string name, List<string> itemIds)
    {
        var update = Builders<Outfit>.Update
            .Set(o => o.Name, name)
            .Set(o => o.ItemIds, itemIds);

        var result = await _db.Outfits.UpdateOneAsync(o => o.Id == id && o.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _db.Outfits.DeleteOneAsync(o => o.Id == id && o.UserId == userId);
        if (result.DeletedCount == 0) return false;

        // Cascade: clean up any worn-log entries pointing at the deleted
        // outfit so the calendar doesn't end up with dangling references.
        await _db.WornLogs.DeleteManyAsync(w => w.OutfitId == id && w.UserId == userId);
        return true;
    }

    public async Task<long> CountOwnedItemsAsync(string userId, List<string> itemIds) =>
        await _db.ClothingItems.CountDocumentsAsync(i => i.UserId == userId && itemIds.Contains(i.Id));
}
