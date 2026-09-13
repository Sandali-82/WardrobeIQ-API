namespace WardrobeApi.DTOs;

// All measurements in the same unit (cm or inches) - only ratios matter.
public record BodyShapeRequest(
    double ShoulderWidth,
    double BustWidth,
    double WaistWidth,
    double HipWidth
);

public record BodyShapeResponse(string BodyShape, string Explanation);