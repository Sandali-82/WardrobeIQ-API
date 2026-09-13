using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WardrobeApi.Models;

public class WornLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string OutfitId { get; set; } = null!;

    // Store as date-only (midnight UTC) so "one outfit per day" lookups are easy
    public DateTime DateWorn { get; set; }
}
