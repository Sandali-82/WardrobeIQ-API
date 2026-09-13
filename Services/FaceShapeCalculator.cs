namespace WardrobeApi.Services;

// Pure ratio-based classification - no ML/CV involved. The user supplies
// four measurements (e.g. measured with a tape measure, or estimated from
// a photo manually), and we classify using well-established anthropometric
// ratio rules used in styling guides.
public static class FaceShapeCalculator
{
    public static (string Shape, string Explanation) Classify(
        double foreheadWidth, double cheekboneWidth, double jawlineWidth, double faceLength)
    {
        double lengthToWidthRatio = faceLength / cheekboneWidth;
        double foreheadToJawDiff = Math.Abs(foreheadWidth - jawlineWidth);
        bool cheekIsWidest = cheekboneWidth > foreheadWidth && cheekboneWidth > jawlineWidth;

        // Oblong: face is noticeably longer than it is wide
        if (lengthToWidthRatio >= 1.5)
            return ("oblong", "Your face is noticeably longer than it is wide, giving an elongated, oblong shape.");

        // Round: length ~= width, forehead and jaw are similar width, cheekbones are widest
        if (lengthToWidthRatio < 1.3 && foreheadToJawDiff < cheekboneWidth * 0.1 && cheekIsWidest)
            return ("round", "Your face length and width are close, with soft, rounded cheekbones as the widest point.");

        // Heart: forehead is the widest point, tapering down to a narrower jaw
        if (foreheadWidth > cheekboneWidth && foreheadWidth > jawlineWidth)
            return ("heart", "Your forehead is the widest point, tapering to a narrower jawline - a classic heart shape.");

        // Square: forehead, cheekbones, and jaw are all similar width, with a strong jawline
        if (foreheadToJawDiff < cheekboneWidth * 0.1 && jawlineWidth >= cheekboneWidth * 0.9)
            return ("square", "Your forehead, cheekbones, and jawline are similarly wide, giving a strong, angular square shape.");

        // Diamond: cheekbones are clearly the widest, forehead and jaw both taper in
        if (cheekIsWidest && foreheadToJawDiff < cheekboneWidth * 0.15)
            return ("diamond", "Your cheekbones are the widest point, with both your forehead and jaw tapering in - a diamond shape.");

        // Default / balanced proportions
        return ("oval", "Your proportions are balanced and gently tapered - a classic oval shape.");
    }
}

