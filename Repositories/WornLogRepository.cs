using MongoDB.Driver;
using WardrobeApi.Models;
using WardrobeApi.Services;

namespace WardrobeApi.Repositories;

public class WornLogRepository : IWornLogRepository
{
    private readonly MongoDbContext _db;

    public WornLogRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<WornLog>> GetRangeForUserAsync(string userId, DateTime? from, DateTime? to)
    {
        var filterBuilder = Builders<WornLog>.Filter;
        var filter = filterBuilder.Eq(w => w.UserId, userId);

        if (from.HasValue)
            filter &= filterBuilder.Gte(w => w.DateWorn, from.Value.Date);
        if (to.HasValue)
            filter &= filterBuilder.Lte(w => w.DateWorn, to.Value.Date);

        return await _db.WornLogs.Find(filter).SortBy(w => w.DateWorn).ToListAsync();
    }

    public Task<Outfit?> GetOwnedOutfitAsync(string outfitId, string userId) =>
        _db.Outfits.Find(o => o.Id == outfitId && o.UserId == userId).FirstOrDefaultAsync()!;

    public Task<WornLog?> GetByUserAndDateAsync(string userId, DateTime dateOnly) =>
        _db.WornLogs.Find(w => w.UserId == userId && w.DateWorn == dateOnly).FirstOrDefaultAsync()!;

    public Task UpdateOutfitForExistingLogAsync(string logId, string newOutfitId)
    {
        var update = Builders<WornLog>.Update.Set(w => w.OutfitId, newOutfitId);
        return _db.WornLogs.UpdateOneAsync(w => w.Id == logId, update);
    }

    public async Task<WornLog> CreateAsync(WornLog log)
    {
        await _db.WornLogs.InsertOneAsync(log);
        return log;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _db.WornLogs.DeleteOneAsync(w => w.Id == id && w.UserId == userId);
        return result.DeletedCount > 0;
    }

    public async Task<Dictionary<string, string>> GetOutfitNamesByIdAsync(List<string> outfitIds)
    {
        var outfits = await _db.Outfits.Find(o => outfitIds.Contains(o.Id)).ToListAsync();
        return outfits.ToDictionary(o => o.Id, o => o.Name);
    }
}
