namespace WardrobeApi.Services;

// Same approach as FaceShapeCalculator: pure ratio-based rules, no ML/CV.
// Classic styling-guide categories: hourglass, pear, apple, rectangle, inverted triangle.
public static class BodyShapeCalculator
{
    public static (string Shape, string Explanation) Classify(
        double shoulderWidth, double bustWidth, double waistWidth, double hipWidth)
    {
        double shoulderHipDiff = Math.Abs(shoulderWidth - hipWidth);
        double bustHipDiff = Math.Abs(bustWidth - hipWidth);
        double waistDefinition = Math.Min(bustWidth, hipWidth) - waistWidth; // how much the waist "comes in"

        // Hourglass: bust and hips are close in width, waist is clearly narrower than both
        if (bustHipDiff <= bustWidth * 0.15 && waistDefinition >= bustWidth * 0.15)
            return ("hourglass", "Your bust and hips are balanced with a clearly defined waist - a classic hourglass shape.");

        // Pear: hips are noticeably wider than bust/shoulders
        if (hipWidth > bustWidth * 1.1 && hipWidth > shoulderWidth * 1.1)
            return ("pear", "Your hips are wider than your bust and shoulders, giving a pear (triangle) shape.");

        // Inverted triangle: shoulders/bust are noticeably wider than hips
        if (shoulderWidth > hipWidth * 1.1 || bustWidth > hipWidth * 1.15)
            return ("inverted triangle", "Your shoulders and bust are wider than your hips, giving an inverted-triangle shape.");

        // Apple: waist is close to or wider than bust/hips (less waist definition, fuller midsection)
        if (waistDefinition < bustWidth * 0.05)
            return ("apple", "Your waist is fuller relative to your bust and hips, giving an apple shape.");

        // Rectangle: bust, waist, and hips are all fairly similar with little waist definition
        return ("rectangle", "Your bust, waist, and hips are similar in width with a soft, straight silhouette - a rectangle shape.");
    }
}
