namespace WardrobeApi.DTOs;

public record CreateOutfitRequest(string Name, List<string> ItemIds);

public record OutfitResponse(
    string Id,
    string Name,
    List<string> ItemIds,
    DateTime CreatedAt
);

