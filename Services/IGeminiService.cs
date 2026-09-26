using WardrobeApi.Models;

namespace WardrobeApi.Services;

public interface IGeminiService
{
    Task<(List<string> ItemIds, string Explanation)> SuggestOutfitAsync(
        List<ClothingItem> wardrobe, string occasion, string? notes,
        string? faceShape = null, string? bodyShape = null, string? skinUndertone = null);

    Task<(string Undertone, string Explanation)> AnalyzeSkinUndertoneAsync(
        string imageBase64, string mimeType);

    Task<(string Necklines, string Hairstyles, string Sleeves, string Silhouettes, string Colors, string Avoid)>
        GetStylingGuideAsync(string? faceShape, string? bodyShape, string? skinUndertone);
}
