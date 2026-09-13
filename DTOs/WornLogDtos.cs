namespace WardrobeApi.DTOs;

public record CreateWornLogRequest(string OutfitId, DateTime DateWorn);

public record WornLogResponse(
    string Id,
    string OutfitId,
    string OutfitName,
    DateTime DateWorn
);
