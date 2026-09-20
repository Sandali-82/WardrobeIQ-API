using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WardrobeApi.Models;

// Keep categories/seasons as plain strings (not enums) so the frontend
// can send new values without a backend redeploy.
public class ClothingItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    // top, bottom, dress,footwear, outerwear, accessory
    public string Category { get; set; } = null!;

    // primary color family, e.g. "black", "white", "blue" - used by the
    // suggestion/color-matching engine, so keep this normalized/lowercase.
    public string Color { get; set; } = null!;

    // summer, winter, all-season, etc.
    public string Season { get; set; } = "all-season";

    public List<string> Tags { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
