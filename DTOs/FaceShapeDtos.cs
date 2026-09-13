namespace WardrobeApi.DTOs;

// All measurements in the same unit (cm or inches - doesn't matter,
// since the calculation only uses ratios between them).
public record FaceShapeRequest(
    double ForeheadWidth,
    double CheekboneWidth,
    double JawlineWidth,
    double FaceLength
);

public record FaceShapeResponse(string FaceShape, string Explanation);

