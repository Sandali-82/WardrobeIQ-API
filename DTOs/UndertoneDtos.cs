namespace WardrobeApi.DTOs;

// Frontend sends the photo as a base64 string (Flutter can read the file
// bytes and base64-encode them before sending - simpler than multipart
// for a single small image, and keeps this endpoint consistent with a
// plain JSON body like the rest of the API).
public record UndertoneRequest(string ImageBase64, string MimeType);

public record UndertoneResponse(string Undertone, string Explanation);

