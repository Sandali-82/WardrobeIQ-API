using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WardrobeApi.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    // Set once the user runs the face-shape calculator; reused on every
    // future suggestion request so they don't have to re-enter measurements.
    public string? FaceShape { get; set; }
    public string? BodyShape { get; set; }
    public string? SkinUndertone { get; set; } // warm / cool / neutral - from a photo via Gemini vision


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
