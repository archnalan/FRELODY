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

        public async Task<YouTubeTranscriptionDto> AnalyzeAsync(YouTubeAnalyzeRequest request, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            // 1. Download audio via yt-dlp sidecar running inside chordmini container
            var extractResp = await _ytdlp.PostAsync(
                "/api/ytdlp/extract",
                JsonContent(new { videoId = request.VideoId }),
                ct);

            if (!extractResp.IsSuccessStatusCode)
            {
                var raw = await extractResp.Content.ReadAsStringAsync(ct);
                string msg;
                try
                {
                    using var errDoc = JsonDocument.Parse(raw);
                    msg = errDoc.RootElement.TryGetProperty("error", out var ep)
                        ? ep.GetString() ?? raw
                        : raw;
                }
                catch { msg = raw; }
                throw new InvalidOperationException(msg);
            }

            var extractBody = await extractResp.Content.ReadAsStringAsync(ct);
            using var extractDoc = JsonDocument.Parse(extractBody);
            var filePath = extractDoc.RootElement.GetProperty("filePath").GetString()
                ?? throw new InvalidOperationException("yt-dlp extract returned no filePath");

            try
            {
                // 2. Detect beats — pass file path directly (same container filesystem)
                var beatsResult = await PostAudioPathAsync<BeatsResult>(
                    "/api/detect-beats", filePath,
                    new Dictionary<string, string> { ["detector"] = request.BeatModel },
                    ct);

                // 3. Recognize chords — same file path
                var chordsResult = await PostAudioPathAsync<ChordsResult>(
                    "/api/recognize-chords", filePath,
                    new Dictionary<string, string>
                    {
                        ["detector"] = request.ChordModel,
                        ["chord_dict"] = request.ChordDict
                    },
                    ct);

                sw.Stop();

                var beats = beatsResult.Beats ?? [];
                var chords = chordsResult.Chords ?? [];

                return new YouTubeTranscriptionDto
                {
                    VideoId = request.VideoId,
                    BeatModel = request.BeatModel,
                    ChordModel = request.ChordModel,
                    ChordDict = request.ChordDict,
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
            [JsonPropertyName("time")] public float Time { get; set; }
            [JsonPropertyName("chord")] public string Chord { get; set; } = default!;
            [JsonPropertyName("confidence")] public float Confidence { get; set; }
        }
    }
}
