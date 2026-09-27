using WardrobeApi.Models;

namespace WardrobeApi.Repositories;

public interface IClothingItemRepository
{
    Task<List<ClothingItem>> GetAllForUserAsync(string userId, string? category, string? color);
    Task<ClothingItem?> GetByIdAsync(string id, string userId);
    Task CreateAsync(ClothingItem item);

    // Returns true if a matching item was found and updated (false = not
    // found / not owned by this user, so the controller can return 404).
    Task<bool> UpdateAsync(string id, string userId, ClothingItem updatedFields);

    Task<bool> DeleteAsync(string id, string userId);
}
