using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using SkiaSharp;
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

        // ── Text-only model ──
        private const string TextModel = "meta/llama-3.1-8b-instruct";

        // ── Vision models (switch ActiveVisionModel to test) ──
        private const string NemotronNano12bVL   = "nvidia/nemotron-nano-12b-v2-vl";           // 12B – NVIDIA's own VLM, max 8192 tokens
        private const string Llama4Maverick       = "meta/llama-4-maverick-17b-128e-instruct";  // 17B 128-expert MoE, most capable
        private const string Llama4Scout          = "meta/llama-4-scout-17b-16e-instruct";      // 17B 16-expert MoE, lighter variant
        private const string Gemma3_27b           = "google/gemma-3-27b-it";                    // 27B, strong image reasoning
        private const string Gemma3n_e4b          = "google/gemma-3n-e4b-it";                   // 4B effective, fast & lightweight

        private const string ActiveVisionModel = NemotronNano12bVL;

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

        public async Task<ServiceResult<List<SegmentCreateDto>>> RefineExtractionAsync(string originalContent, List<SegmentCreateDto> segments, string? imageBase64 = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "MY_NVIDIA_API")
            {
                return ServiceResult<List<SegmentCreateDto>>.Success(segments);
            }

            try
            {
                var segmentsJson = JsonSerializer.Serialize(segments, _jsonOptions);
                var prompt = BuildPrompt(originalContent, segmentsJson);

                var hasImage = !string.IsNullOrWhiteSpace(imageBase64);
                var responseText = hasImage
                    ? await CallNvidiaVisionApi(prompt, imageBase64!)
                    : await CallNvidiaApi(prompt);
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
                model = TextModel,
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

        /// <summary>
        /// Calls a NVIDIA vision model with an image (base64) + text prompt.
        /// The image is embedded in the user content via an HTML img tag as per NVIDIA NIM API.
        /// </summary>
        private async Task<string> CallNvidiaVisionApi(string prompt, string imageBase64)
        {
            var client = _httpClientFactory.CreateClient("NvidiaAI");

            // Downscale if needed — NVIDIA inline base64 is tokenized char-by-char,
            // so large images blow past the 131K token context window.
            const int maxBase64Bytes = 180_000; // ~180 KB keeps us well under token limit
            var originalSizeKb = imageBase64.Length * 3 / 4 / 1024;

            if (imageBase64.Length > maxBase64Bytes)
            {
                _logger.LogInformation(
                    "[AI-Vision] Image too large (~{OriginalKB} KB), downscaling to fit token limit",
                    originalSizeKb);
                imageBase64 = DownscaleImageBase64(imageBase64, maxBase64Bytes);
                _logger.LogInformation(
                    "[AI-Vision] Downscaled to ~{NewKB} KB",
                    imageBase64.Length * 3 / 4 / 1024);
            }

            // Always jpeg after potential downscale
            var imageFormat = "jpeg";
            if (imageBase64.StartsWith("iVBOR", StringComparison.Ordinal))
                imageFormat = "png";

            var imageSizeKb = imageBase64.Length * 3 / 4 / 1024;
            _logger.LogInformation(
                "[AI-Vision] Preparing request — model: {Model}, format: {Format}, image ~{SizeKB} KB, prompt length: {PromptLength} chars",
                ActiveVisionModel, imageFormat, imageSizeKb, prompt.Length);

            // NVIDIA NIM vision models accept base64 images inline via <img> tags in the content string
            var userContent = $"<img src=\"data:image/{imageFormat};base64,{imageBase64}\" />\n{prompt}";

            var requestBody = new
            {
                model = ActiveVisionModel,
                messages = new[]
                {
                    new { role = "user", content = userContent }
                },
                temperature = 0.2,
                max_tokens = 4096
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[AI-Vision] Sending request — payload size: {PayloadKB} KB", json.Length / 1024);

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = content
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "[AI-Vision] API returned {StatusCode} — body: {ErrorBody}",
                    (int)response.StatusCode, errorBody.Length > 500 ? errorBody[..500] : errorBody);
                response.EnsureSuccessStatusCode(); // still throw for the caller
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation("[AI-Vision] Success — response content length: {Length} chars", messageContent?.Length ?? 0);

            return messageContent ?? string.Empty;
        }

        /// <summary>
        /// Progressively downscales a base64 image (JPEG) until it fits within the target byte size.
        /// Uses SkiaSharp for cross-platform image processing.
        /// </summary>
        private string DownscaleImageBase64(string imageBase64, int maxBase64Bytes)
        {
            var imageBytes = Convert.FromBase64String(imageBase64);
            using var original = SKBitmap.Decode(imageBytes);

            if (original == null)
            {
                _logger.LogWarning("[AI-Vision] Failed to decode image for downscaling, using original");
                return imageBase64;
            }

            var width = original.Width;
            var height = original.Height;
            var quality = 80;

            // Iteratively shrink: reduce dimensions by 30% each pass, lower quality if needed
            for (int attempt = 0; attempt < 6; attempt++)
            {
                using var resized = original.Resize(new SKImageInfo(width, height), SKSamplingOptions.Default);

                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                var base64 = Convert.ToBase64String(data.ToArray());

                _logger.LogInformation(
                    "[AI-Vision] Downscale attempt {Attempt}: {Width}x{Height} q{Quality} → ~{SizeKB} KB",
                    attempt + 1, width, height, quality, base64.Length * 3 / 4 / 1024);

                if (base64.Length <= maxBase64Bytes)
                    return base64;

                // Shrink further
                width = (int)(width * 0.7);
                height = (int)(height * 0.7);
                quality = Math.Max(50, quality - 10);
            }

            // Last resort: very small
            using var lastResized = original.Resize(new SKImageInfo(width, height), SKSamplingOptions.Default);
            if (lastResized != null)
            {
                using var lastImage = SKImage.FromBitmap(lastResized);
                using var lastData = lastImage.Encode(SKEncodedImageFormat.Jpeg, 40);
                return Convert.ToBase64String(lastData.ToArray());
            }

            return imageBase64;
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

        public async Task<string?> RefineOcrTextAsync(string ocrText, string imageBase64)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "MY_NVIDIA_API")
            {
                _logger.LogWarning("[AI-Refine] Skipped — API key not configured");
                return null;
            }

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("[AI-Refine] Skipped — empty OCR text");
                return null;
            }

            try
            {
                var hasImage = !string.IsNullOrWhiteSpace(imageBase64);
                _logger.LogInformation(
                    "[AI-Refine] Starting — model: {Model}, hasImage: {HasImage}, ocrText length: {OcrLength} chars",
                    ActiveVisionModel, hasImage, ocrText.Length);

                var prompt = BuildOcrRefinementPrompt(ocrText);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                // Use the vision model when we have the scanned image so the LLM
                // can cross-reference the actual image with the OCR text output.
                var responseText = hasImage
                    ? await CallNvidiaVisionApi(prompt, imageBase64)
                    : await CallNvidiaApi(prompt);
                sw.Stop();

                _logger.LogInformation(
                    "[AI-Refine] API call completed in {ElapsedMs}ms — response length: {ResponseLength} chars",
                    sw.ElapsedMilliseconds, responseText?.Length ?? 0);

                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    // Strip markdown fences if present
                    var trimmed = responseText.Trim();
                    if (trimmed.StartsWith("```"))
                    {
                        var firstNewline = trimmed.IndexOf('\n');
                        if (firstNewline > 0)
                            trimmed = trimmed[(firstNewline + 1)..];
                        if (trimmed.EndsWith("```"))
                            trimmed = trimmed[..^3];
                        trimmed = trimmed.Trim();
                        _logger.LogInformation("[AI-Refine] Stripped markdown fences from response");
                    }
                    _logger.LogInformation("[AI-Refine] Refined text preview: {Preview}",
                        trimmed.Length > 200 ? trimmed[..200] + "…" : trimmed);
                    return trimmed;
                }

                _logger.LogWarning("[AI-Refine] API returned empty response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI-Refine] Failed (model: {Model})", ActiveVisionModel);
            }

            return null;
        }

        private static string BuildOcrRefinementPrompt(string ocrText)
        {
            return $@"You are a music transcription expert. You have an image of a song sheet and OCR text extracted from that same image.

CRITICAL: Transcribe ONLY what is actually visible in the image. Do NOT guess, complete, or invent any content. If something is illegible, write [illegible] instead of guessing. If the image contains only a few words, your output should also contain only those few words.

Use the image as the primary source of truth. The OCR text below may help you decipher unclear characters, but it may also contain errors.

OUTPUT FORMAT — ChordPro inline notation:
- Title on the first line if visible (include song number if present)
- Section headers on their own line if visible (e.g. ""Verse 1:"", ""Chorus:"")
- Chords in square brackets before the syllable: [G]A-men [F]A-men
- Use hyphens for syllable breaks: ""Hallel-u-jah""
- Separate sections with a blank line
- Do NOT include metadata (Hymn No, Category, Time Signature)
- Do NOT add any chords, lyrics, sections, or titles that are not in the image

RULES:
1. ONLY output what you can see in the image — nothing more
2. Fix OCR misreads by cross-referencing the image (e.g. ""8m""→""Bm"", ""Arn""→""Am"")
3. Normalize chord names (""Amin""→""Am"", ""Cmaj""→""C"")
4. If the image has very little content, return very little output
5. Never fabricate or complete partial songs from memory

OCR EXTRACTED TEXT (may contain errors):
{ocrText}

Return ONLY the corrected song text from the image. No explanations, commentary, or code blocks.";
        }
    }
}
