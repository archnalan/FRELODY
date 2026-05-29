using System.Text.Json;
using FRELODYSHRD.Dtos;

namespace FRELODYUI.Shared.Pages.Discover;

// Parses the AnalyzedAccessResultDto carried in a 402 Payment Required response
// body from the analyze/transcription endpoints.
internal static class AnalyzedAccessParser
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static AnalyzedAccessResultDto? Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            return JsonSerializer.Deserialize<AnalyzedAccessResultDto>(body, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
