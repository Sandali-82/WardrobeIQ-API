using System.Text;
using System.Text.Json;
using WardrobeApi.Models;

namespace WardrobeApi.Services;

// Talks to Google's Gemini API for:
// 1) reasoning over a wardrobe to suggest an outfit for an occasion
// 2) analyzing a photo to estimate skin undertone (warm/cool/neutral)
// 3) a one-time general styling guide (necklines/hair/sleeves/colors) built
//    from face shape + body shape + undertone, independent of any occasion -
//    this is the "browse once, reuse forever" reference the user asked for,
//    instead of them re-researching styling advice for every outfit choice.
public class GeminiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<(List<string> ItemIds, string Explanation)> SuggestOutfitAsync(
        List<ClothingItem> wardrobe, string occasion, string? notes,
        string? faceShape = null, string? bodyShape = null, string? skinUndertone = null)
    {
        var apiKey = RequireApiKey();

        var wardrobeForPrompt = wardrobe.Select(i => new
        {
            id = i.Id,
            name = i.Name,
            category = i.Category,
            color = i.Color,
            season = i.Season
        });

        var prompt = $$"""
            You are a fashion assistant. A user has the following wardrobe items (JSON array):
            {{JsonSerializer.Serialize(wardrobeForPrompt)}}

            Occasion: {{occasion}}
            Extra notes from the user: {{(string.IsNullOrWhiteSpace(notes) ? "none" : notes)}}
            User's face shape: {{(string.IsNullOrWhiteSpace(faceShape) ? "unknown - ignore this factor" : faceShape)}}
            User's body shape: {{(string.IsNullOrWhiteSpace(bodyShape) ? "unknown - ignore this factor" : bodyShape)}}
            User's skin undertone: {{(string.IsNullOrWhiteSpace(skinUndertone) ? "unknown - ignore this factor" : skinUndertone)}}

            Pick the items (by "id") from the wardrobe above that best form ONE complete outfit
            for this occasion. If face shape, body shape, and/or skin undertone are known, factor
            in established styling guidance (necklines for face shape, silhouettes for body shape,
            color choices for undertone) and briefly mention that reasoning in the explanation.
            Only use ids that appear in the list above - never invent ids.
            Respond with ONLY valid JSON in this exact shape, no markdown fences:
            {"itemIds": ["<id1>", "<id2>"], "explanation": "<one short paragraph explaining the choice>"}
            """;

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { response_mime_type = "application/json" }
        };

        var raw = await SendGeminiRequestAsync(apiKey, requestBody);
        var text = ExtractTextFromResponse(raw);

        using var parsed = JsonDocument.Parse(text);
        var itemIds = parsed.RootElement.GetProperty("itemIds")
            .EnumerateArray().Select(e => e.GetString()!).ToList();
        var explanation = parsed.RootElement.GetProperty("explanation").GetString() ?? "";

        return (itemIds, explanation);
    }

    public async Task<(string Undertone, string Explanation)> AnalyzeSkinUndertoneAsync(
        string imageBase64, string mimeType)
    {
        var apiKey = RequireApiKey();

        var prompt = """
            Look at the skin visible in this photo (face, neck, or inner wrist) and estimate
            the person's skin undertone as one of: "warm", "cool", or "neutral".
            Respond with ONLY valid JSON in this exact shape, no markdown fences:
            {"undertone": "warm|cool|neutral", "explanation": "<one short sentence on what you observed>"}
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = imageBase64 } }
                    }
                }
            },
            generationConfig = new { response_mime_type = "application/json" }
        };

        var raw = await SendGeminiRequestAsync(apiKey, requestBody);
        var text = ExtractTextFromResponse(raw);

        using var parsed = JsonDocument.Parse(text);
        var undertone = parsed.RootElement.GetProperty("undertone").GetString() ?? "neutral";
        var explanation = parsed.RootElement.GetProperty("explanation").GetString() ?? "";

        return (undertone, explanation);
    }

    // One-off, occasion-independent styling reference built from the user's
    // saved face shape + body shape + undertone. Cheaper than calling the
    // occasion endpoint repeatedly just to ask "what suits me in general?".
    public async Task<(string Necklines, string Hairstyles, string Sleeves, string Silhouettes, string Colors, string Avoid)>
        GetStylingGuideAsync(string? faceShape, string? bodyShape, string? skinUndertone)
    {
        var apiKey = RequireApiKey();

        var prompt = $$"""
            You are a fashion/styling assistant. Build a general styling reference guide for
            a user with these attributes:
            Face shape: {{(string.IsNullOrWhiteSpace(faceShape) ? "unknown - skip this factor" : faceShape)}}
            Body shape: {{(string.IsNullOrWhiteSpace(bodyShape) ? "unknown - skip this factor" : bodyShape)}}
            Skin undertone: {{(string.IsNullOrWhiteSpace(skinUndertone) ? "unknown - skip this factor" : skinUndertone)}}

            Give concise, practical guidance (1-2 sentences each) covering:
            - necklines/collars that suit the face shape
            - hairstyles that suit the face shape
            - sleeve styles that suit the body shape
            - overall silhouettes/cuts that suit the body shape
            - colors that suit the skin undertone
            - a short list of things to avoid across all three factors

            Respond with ONLY valid JSON in this exact shape, no markdown fences:
            {"necklines": "...", "hairstyles": "...", "sleeves": "...", "silhouettes": "...", "colors": "...", "avoid": "..."}
            """;

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { response_mime_type = "application/json" }
        };

        var raw = await SendGeminiRequestAsync(apiKey, requestBody);
        var text = ExtractTextFromResponse(raw);

        using var parsed = JsonDocument.Parse(text);
        string Get(string field) => parsed.RootElement.GetProperty(field).GetString() ?? "";

        return (Get("necklines"), Get("hairstyles"), Get("sleeves"), Get("silhouettes"), Get("colors"), Get("avoid"));
    }

    private string RequireApiKey()
    {
        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Gemini:ApiKey is not configured in appsettings.json / user secrets.");
        return apiKey;
    }

    private async Task<string> SendGeminiRequestAsync(string apiKey, object requestBody)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        var jsonBody = JsonSerializer.Serialize(requestBody);

        const int maxAttempts = 3;
        HttpResponseMessage? response = null;
        string raw = "";

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            response = await _http.PostAsync(url,
                new StringContent(jsonBody, Encoding.UTF8, "application/json"));

            raw = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return raw;

            bool isTransient = response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                             || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests;

            if (!isTransient || attempt == maxAttempts)
                throw new InvalidOperationException($"Gemini API error ({response.StatusCode}): {raw}");

            await Task.Delay(1000 * attempt);
        }

        return raw;
    }

    private static string ExtractTextFromResponse(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";
    }
}
