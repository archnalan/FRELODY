using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FRELODYAPIs.Services.ChordMini
{
    public class ChordMiniService : IChordMiniService
    {
        private readonly HttpClient _http;
        private readonly YoutubeClient _youtube = new();

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ChordMiniService(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("ChordMini");
        }

        public async Task<YouTubeTranscriptionDto> AnalyzeAsync(YouTubeAnalyzeRequest request, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var tempFile = Path.Combine(Path.GetTempPath(), $"frelody_{request.VideoId}_{Guid.NewGuid():N}.tmp");

            try
            {
                // 1. Download best audio stream from YouTube to a temp file
                var manifest = await _youtube.Videos.Streams.GetManifestAsync(request.VideoId, ct);
                var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, tempFile, cancellationToken: ct);

                var audioFileName = $"{request.VideoId}.{streamInfo.Container.Name}";

                // 2. Detect beats
                var beatsResult = await PostAudioAsync<BeatsResult>(
                    "/api/detect-beats", tempFile, audioFileName,
                    new Dictionary<string, string> { ["detector"] = request.BeatModel },
                    ct);

                // 3. Recognize chords
                var chordsResult = await PostAudioAsync<ChordsResult>(
                    "/api/recognize-chords", tempFile, audioFileName,
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
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        private async Task<T> PostAudioAsync<T>(
            string endpoint, string filePath, string fileName,
            Dictionary<string, string> fields, CancellationToken ct)
        {
            using var form = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            form.Add(fileContent, "file", fileName);

            foreach (var (key, value) in fields)
                form.Add(new StringContent(value), key);

            using var resp = await _http.PostAsync(endpoint, form, ct);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<T>(body, _json)
                ?? throw new InvalidOperationException($"Null response from {endpoint}. Body: {body}");
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

        // ── Internal response shapes ──────────────────────────────────────────

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
