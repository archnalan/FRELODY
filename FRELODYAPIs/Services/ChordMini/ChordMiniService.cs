using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Services.ChordMini
{
    public class ChordMiniService : IChordMiniService
    {
        private readonly HttpClient _chordmini;
        private readonly HttpClient _ytdlp;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ChordMiniService(IHttpClientFactory factory)
        {
            _chordmini = factory.CreateClient("ChordMini");
            _ytdlp = factory.CreateClient("ChordMiniYtdlp");
        }

        public Task<YouTubeTranscriptionDto> AnalyzeAsync(YouTubeAnalyzeRequest request, CancellationToken ct = default)
            => RunPipelineAsync(
                new { videoId = request.VideoId }, request.VideoId,
                request.BeatModel, request.ChordModel, request.ChordDict, ct);

        public Task<YouTubeTranscriptionDto> AnalyzeUrlAsync(
            string url, string idForResult,
            string beatModel, string chordModel, string chordDict,
            CancellationToken ct = default)
            => RunPipelineAsync(
                new { url }, idForResult, beatModel, chordModel, chordDict, ct);

        public async Task<MediaInfo> GetInfoAsync(string url, CancellationToken ct = default)
        {
            var resp = await _ytdlp.PostAsync("/api/ytdlp/info", JsonContent(new { url }), ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(ExtractError(body));

            var info = JsonSerializer.Deserialize<YtInfo>(body, _json)
                ?? throw new InvalidOperationException("Empty metadata response.");
            return new MediaInfo(
                info.Id, info.Title, info.Uploader,
                info.Thumbnail, info.DurationSeconds, info.WebpageUrl ?? url);
        }

        private async Task<YouTubeTranscriptionDto> RunPipelineAsync(
            object extractBody, string idForResult,
            string beatModel, string chordModel, string chordDict,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            // 1. Download audio via yt-dlp sidecar running inside chordmini container
            var extractResp = await _ytdlp.PostAsync("/api/ytdlp/extract", JsonContent(extractBody), ct);

            if (!extractResp.IsSuccessStatusCode)
            {
                var raw = await extractResp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(ExtractError(raw));
            }

            var extractJson = await extractResp.Content.ReadAsStringAsync(ct);
            using var extractDoc = JsonDocument.Parse(extractJson);
            var filePath = extractDoc.RootElement.GetProperty("filePath").GetString()
                ?? throw new InvalidOperationException("yt-dlp extract returned no filePath");

            try
            {
                // 2. Detect beats — pass file path directly (same container filesystem)
                var beatsResult = await PostAudioPathAsync<BeatsResult>(
                    "/api/detect-beats", filePath,
                    new Dictionary<string, string> { ["detector"] = beatModel },
                    ct);

                // 3. Recognize chords — same file path
                var chordsResult = await PostAudioPathAsync<ChordsResult>(
                    "/api/recognize-chords", filePath,
                    new Dictionary<string, string>
                    {
                        ["detector"] = chordModel,
                        ["chord_dict"] = chordDict
                    },
                    ct);

                sw.Stop();

                var beats = beatsResult.Beats ?? [];
                var chords = chordsResult.Chords ?? [];

                return new YouTubeTranscriptionDto
                {
                    VideoId = idForResult,
                    BeatModel = beatModel,
                    ChordModel = chordModel,
                    ChordDict = chordDict,
                    Beats = beats,
                    Chords = chords.Select(c => new ChordEventDto
                    {
                        Time = c.Time,
                        Chord = c.Chord,
                        Confidence = c.Confidence
                    }).ToList(),
                    SyncedChords = ComputeSyncedChords(beats, chords),
                    Bpm = EstimateBpm(beats),
                    TimeSignature = beatsResult.TimeSignature,
                    TotalProcessingSeconds = (float)sw.Elapsed.TotalSeconds,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }
            finally
            {
                // Clean up temp file in chordmini container
                try
                {
                    await _ytdlp.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/ytdlp/cleanup")
                    {
                        Content = JsonContent(new { filePath })
                    }, ct);
                }
                catch { /* best-effort */ }
            }
        }

        private async Task<T> PostAudioPathAsync<T>(
            string endpoint, string audioPath,
            Dictionary<string, string> fields, CancellationToken ct)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(audioPath), "audio_path");

            foreach (var (key, value) in fields)
                form.Add(new StringContent(value), key);

            using var resp = await _chordmini.PostAsync(endpoint, form, ct);

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"{endpoint} returned {(int)resp.StatusCode}: {body}");

            return JsonSerializer.Deserialize<T>(body, _json)
                ?? throw new InvalidOperationException($"Null response from {endpoint}. Body: {body}");
        }

        private static System.Net.Http.StringContent JsonContent(object obj) =>
            new(JsonSerializer.Serialize(obj), System.Text.Encoding.UTF8, "application/json");

        // Pull the sidecar's friendly {"error": "..."} message out of a failed body.
        private static string ExtractError(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("error", out var ep))
                    return ep.GetString() ?? raw;
            }
            catch { }
            return raw;
        }

        private sealed class YtInfo
        {
            [JsonPropertyName("id")] public string Id { get; set; } = default!;
            [JsonPropertyName("title")] public string Title { get; set; } = "Untitled";
            [JsonPropertyName("uploader")] public string? Uploader { get; set; }
            [JsonPropertyName("thumbnail")] public string? Thumbnail { get; set; }
            [JsonPropertyName("durationSeconds")] public int DurationSeconds { get; set; }
            [JsonPropertyName("webpageUrl")] public string? WebpageUrl { get; set; }
        }

        private static List<SyncedChordDto> ComputeSyncedChords(List<float> beats, List<RawChordEvent> chords)
        {
            if (beats.Count == 0)
                return chords.Select((c, i) => new SyncedChordDto { Time = c.Time, Chord = c.Chord, BeatIndex = i }).ToList();

            return chords.Select(c =>
            {
                int idx = NearestBeatIndex(beats, c.Time);
                return new SyncedChordDto { Time = c.Time, Chord = c.Chord, BeatIndex = idx };
            }).ToList();
        }

        private static int NearestBeatIndex(List<float> sorted, float target)
        {
            int lo = 0, hi = sorted.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (sorted[mid] < target) lo = mid + 1; else hi = mid;
            }
            if (lo > 0 && Math.Abs(sorted[lo - 1] - target) <= Math.Abs(sorted[lo] - target))
                return lo - 1;
            return lo;
        }

        private static float? EstimateBpm(List<float> beats)
        {
            if (beats.Count < 2) return null;
            float avg = beats.Zip(beats.Skip(1), (a, b) => b - a).Sum() / (beats.Count - 1);
            return avg > 0 ? MathF.Round(60f / avg, 1) : null;
        }

        private sealed class BeatsResult
        {
            [JsonPropertyName("beats")] public List<float> Beats { get; set; } = [];
            [JsonPropertyName("time_signature")] public string? TimeSignature { get; set; }
        }

        private sealed class ChordsResult
        {
            [JsonPropertyName("chords")] public List<RawChordEvent> Chords { get; set; } = [];
        }

        private sealed class RawChordEvent
        {
            // ChordMini's /api/recognize-chords returns each chord as a
            // {chord, confidence, start, end} span — the onset is "start"
            // (there is no "time" field). Mapping the wrong key here parks
            // every chord at 0s and breaks playback sync.
            [JsonPropertyName("start")] public float Time { get; set; }
            [JsonPropertyName("end")] public float End { get; set; }
            [JsonPropertyName("chord")] public string Chord { get; set; } = default!;
            [JsonPropertyName("confidence")] public float Confidence { get; set; }
        }
    }
}
