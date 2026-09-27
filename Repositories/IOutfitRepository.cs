using WardrobeApi.Models;

namespace WardrobeApi.Repositories;

public interface IOutfitRepository
{
    Task<List<Outfit>> GetAllForUserAsync(string userId);
    Task<Outfit?> GetByIdAsync(string id, string userId);
    Task CreateAsync(Outfit outfit);
    Task<bool> UpdateAsync(string id, string userId, string name, List<string> itemIds);
    Task<bool> DeleteAsync(string id, string userId);

    // How many of the given item ids actually belong to this user - the
    // controller compares this against the requested count to validate
    // ownership before creating/updating an outfit.
    Task<long> CountOwnedItemsAsync(string userId, List<string> itemIds);
}
