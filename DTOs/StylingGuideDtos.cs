namespace WardrobeApi.DTOs;

public record StylingGuideResponse(
    string Necklines,
    string Hairstyles,
    string Sleeves,
    string Silhouettes,
    string Colors,
    string Avoid
);