namespace WardrobeApi.DTOs;

// Occasion is now a constrained set of values instead of free text, so
// Gemini gets consistent, predictable input and the Flutter app can show a
// simple dropdown/chip selector instead of a text field.
public static class OccasionTypes
{
    public static readonly string[] Allowed = { "casual", "formal", "business", "party", "wedding", "sport" };
}

public record OccasionSuggestionRequest(string Occasion, string? Notes);

// What we return after asking Gemini to reason over the user's wardrobe
public record OutfitSuggestionResponse(
    List<string> SuggestedItemIds,
    string Explanation
);


