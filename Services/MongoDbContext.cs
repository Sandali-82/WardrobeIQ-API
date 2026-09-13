using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WardrobeApi.Models;

namespace WardrobeApi.Services;

// Single place that knows about collection names, so controllers/services
// never hardcode "users", "clothingItems" as magic strings.
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<ClothingItem> ClothingItems => _database.GetCollection<ClothingItem>("clothingItems");
    public IMongoCollection<Outfit> Outfits => _database.GetCollection<Outfit>("outfits");
    public IMongoCollection<WornLog> WornLogs => _database.GetCollection<WornLog>("wornLogs");
}
