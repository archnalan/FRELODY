using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class SongAiService : ISongAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _apiKey;
        private readonly ILogger<SongAiService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public SongAiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SongAiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["API_KEYS:nvidiaApiKey"];
            _logger = logger;
        }

        public async Task<ServiceResult<List<SegmentCreateDto>>> RefineExtractionAsync(string originalContent, List<SegmentCreateDto> segments)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "MY_NVIDIA_API")
            {
                return ServiceResult<List<SegmentCreateDto>>.Success(segments);
            }

            try
            {
                var segmentsJson = JsonSerializer.Serialize(segments, _jsonOptions);
                var prompt = BuildPrompt(originalContent, segmentsJson);

                var responseText = await CallNvidiaApi(prompt);
                var refined = ParseAiResponse(responseText, segments);

                return ServiceResult<List<SegmentCreateDto>>.Success(refined);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NVIDIA AI refinement failed");
                return ServiceResult<List<SegmentCreateDto>>.Failure(ex);
            }
        }

        private static string BuildPrompt(string originalContent, string segmentsJson)
        {
            return $@"You are a music expert that refines chord/lyric extraction data for songs.

Given the original song text and the extracted segments (JSON), fix any issues:

1. **Chord placement**: Ensure chords are aligned to the correct syllable/word.
2. **Section detection**: Identify song sections (Verse, Chorus, Bridge, PreChorus, Intro, Outro, Interlude, Solo, Refrain, Coda, PostChorus) from textual cues like ""Chorus:"", ""Verse 1:"", repeated patterns, etc. Update PartName and PartNumber accordingly.
3. **Line grouping**: Ensure LineNumber groups segments that belong on the same displayed line.
4. **LyricOrder**: Ensure segments within each line are in correct left-to-right order (0-based).
5. **Missing chords**: If the original text has chords that were missed, add them.
6. **Consistency**: Normalize chord names (e.g., ""Amin"" → ""Am"", ""Cmaj"" → ""C"").

Valid SongSection values: unknown, Intro, Verse, PreChorus, Chorus, PostChorus, Bridge, Interlude, Solo, Refrain, Coda, Outro

Valid Alignment values: Left, Center, Right

ORIGINAL TEXT:
{originalContent}

EXTRACTED SEGMENTS (JSON):
{segmentsJson}

Return ONLY a valid JSON array of refined segments with the same structure. Each segment must have: Id (nullable string), Lyric (string), LineNumber (int), ChordId (nullable string), ChordName (nullable string), PartNumber (int, 1-based), PartName (SongSection string), LyricOrder (int, 0-based), AddNextSegment (bool), ChordAlignment (Alignment string).

Do not wrap the JSON in markdown code blocks. Return raw JSON only.";
        }

        private async Task<string> CallNvidiaApi(string prompt)
        {
            var client = _httpClientFactory.CreateClient("NvidiaAI");

            var requestBody = new
            {
                model = "meta/llama-3.1-8b-instruct",
                messages = new[]
                {
                    new { role = "system", content = "You are a music transcription assistant. You only respond with valid JSON arrays." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2,
                max_tokens = 1500
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = content
            });

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return messageContent ?? string.Empty;
        }

        private static List<SegmentCreateDto> ParseAiResponse(string responseText, List<SegmentCreateDto> fallback)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return fallback;

            // Strip markdown code fences if the model wraps them anyway
            var trimmed = responseText.Trim();
            if (trimmed.StartsWith("```"))
            {
                var firstNewline = trimmed.IndexOf('\n');
                if (firstNewline > 0)
                    trimmed = trimmed[(firstNewline + 1)..];
                if (trimmed.EndsWith("```"))
                    trimmed = trimmed[..^3];
                trimmed = trimmed.Trim();
            }

            try
            {
                var refined = JsonSerializer.Deserialize<List<SegmentCreateDto>>(trimmed, _jsonOptions);
                if (refined != null && refined.Count > 0)
                    return refined;
            }
            catch (JsonException)
            {
                // AI response wasn't valid JSON — return original
            }

            return fallback;
        }
    }
}
