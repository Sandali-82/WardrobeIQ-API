namespace WardrobeApi.DTOs;

public record CreateClothingItemRequest(
    string Name,
    string ImageUrl,
    string Category,
    string Color,
    string? Season,
    List<string>? Tags
);

public record ClothingItemResponse(
    string Id,
    string Name,
    string ImageUrl,
    string Category,
    string Color,
    string Season,
    List<string> Tags,
    DateTime CreatedAt
);
