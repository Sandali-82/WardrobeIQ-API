using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WardrobeApi.Models;

public class Outfit
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    public string Name { get; set; } = null!;

    // References to ClothingItem._id
    public List<string> ItemIds { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
