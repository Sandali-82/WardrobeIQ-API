using WardrobeApi.Models;

namespace WardrobeApi.Repositories;

public interface IWornLogRepository
{
    Task<List<WornLog>> GetRangeForUserAsync(string userId, DateTime? from, DateTime? to);

    // Returns the outfit if it exists and belongs to this user - callers
    // use this both to validate ownership and to get the outfit's name.
    Task<Outfit?> GetOwnedOutfitAsync(string outfitId, string userId);

    Task<WornLog?> GetByUserAndDateAsync(string userId, DateTime dateOnly);
    Task UpdateOutfitForExistingLogAsync(string logId, string newOutfitId);
    Task<WornLog> CreateAsync(WornLog log);
    Task<bool> DeleteAsync(string id, string userId);

    // Resolves outfit names for a batch of logs in one query (avoids N+1);
    // any id with no matching outfit is simply absent from the result.
    Task<Dictionary<string, string>> GetOutfitNamesByIdAsync(List<string> outfitIds);
}
