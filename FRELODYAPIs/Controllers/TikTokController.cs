using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Services.ChordMini;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TikTokController : ControllerBase
    {
        private readonly SongDbContext _db;
        private readonly IChordMiniService _chordMini;
        private readonly IAnalyzedAccessService _access;

        public TikTokController(SongDbContext db, IChordMiniService chordMini, IAnalyzedAccessService access)
        {
            _db = db;
            _chordMini = chordMini;
            _access = access;
        }

        // Authoritative daily-quota / 24h-availability gate for analyzed playback.
        // Denied is a non-null IActionResult (402 + paywall payload, or an error) when
        // the caller must NOT receive the transcription; null when access is granted.
        // Access carries the evaluation so a downstream failure can refund a consumed slot.
        private async Task<(ActionResult<YouTubeTranscriptionDto>? Denied, AnalyzedAccessResultDto? Access)> GateAnalyzedAccessAsync(string videoId, string? title, string? thumbnailUrl, string? sourceUrl, int? durationSeconds)
        {
            var access = await _access.EvaluateAndRecord(
                AnalyzedPlatform.TikTok, videoId, title, thumbnailUrl, sourceUrl, durationSeconds);

            if (!access.IsSuccess)
                return (StatusCode(access.StatusCode, new { message = access.Error.Message }), null);

            if (!access.Data.Allowed)
                return (StatusCode(StatusCodes.Status402PaymentRequired, access.Data), access.Data);

            return (null, access.Data);
        }

        // Refund a freshly-consumed slot when we ultimately can't hand back chords.
        private async Task ReleaseUnlockIfRecorded(AnalyzedAccessResultDto? access, string videoId)
        {
            if (access?.Recorded == true)
                await _access.ReleaseUnlock(AnalyzedPlatform.TikTok, videoId);
        }

        // Resolve a pasted TikTok URL into cached video metadata (id, title, thumb).
        [HttpPost]
        [ProducesResponseType(typeof(TikTokVideoDto), 200)]
        public async Task<ActionResult<TikTokVideoDto>> Resolve(
            [FromBody] TikTokResolveRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Url) || !IsTikTokUrl(request.Url))
                return BadRequest(new { message = "A valid TikTok URL is required." });

            MediaInfo info;
            try
            {
                info = await _chordMini.GetInfoAsync(request.Url, ct);
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = ex.Message });
            }

            var dto = await UpsertAndMapAsync(info, ct);
            // Dimensions come straight from the fresh yt-dlp probe (not persisted);
            // the playback view uses them to size the player to the clip's ratio.
            dto.Width = info.Width;
            dto.Height = info.Height;
            return Ok(dto);
        }

        [HttpGet]
        [ProducesResponseType(typeof(TikTokVideoDto), 200)]
        public async Task<ActionResult<TikTokVideoDto>> GetVideo([FromQuery] string videoId)
        {
            var v = await _db.TikTokVideos.AsNoTracking()
                .FirstOrDefaultAsync(x => x.VideoId == videoId);
            return v is null
                ? NotFound(new { message = $"Video '{videoId}' not found." })
                : Ok(MapToDto(v));
        }

        [HttpPost]
        [ProducesResponseType(typeof(YouTubeTranscriptionDto), 200)]
        public async Task<ActionResult<YouTubeTranscriptionDto>> Analyze(
            [FromBody] TikTokAnalyzeRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Url) || !IsTikTokUrl(request.Url))
                return BadRequest(new { message = "A valid TikTok URL is required." });

            // Resolve id + metadata first so we can cache by id.
            MediaInfo info;
            try
            {
                info = await _chordMini.GetInfoAsync(request.Url, ct);
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = ex.Message });
            }

            // Meter the analyzed play before returning any chord data (cached or fresh).
            var (denied, access) = await GateAnalyzedAccessAsync(info.Id, Truncate(info.Title, 500), Truncate(info.Thumbnail, 1000), info.WebpageUrl, info.DurationSeconds);
            if (denied is not null)
                return denied;

            if (!request.ForceRefresh)
            {
                var cached = await _db.TikTokTranscriptions.AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.VideoId == info.Id &&
                        t.BeatModel == request.BeatModel &&
                        t.ChordModel == request.ChordModel &&
                        t.ChordDict == request.ChordDict, ct);
                if (cached is not null)
                    return Ok(MapTranscriptionToDto(cached));
            }

            YouTubeTranscriptionDto result;
            try
            {
                result = await _chordMini.AnalyzeUrlAsync(
                    info.WebpageUrl, info.Id,
                    request.BeatModel, request.ChordModel, request.ChordDict, ct);
            }
            catch (Exception ex)
            {
                // Analysis failed — refund the slot so the user keeps their daily song.
                await ReleaseUnlockIfRecorded(access, info.Id);
                return StatusCode(502, new { message = ex.Message });
            }

            // Ensure the parent video row exists (FK), then persist the transcription.
            await UpsertAndMapAsync(info, ct);

            var existing = await _db.TikTokTranscriptions.FirstOrDefaultAsync(t =>
                t.VideoId == info.Id &&
                t.BeatModel == request.BeatModel &&
                t.ChordModel == request.ChordModel &&
                t.ChordDict == request.ChordDict, ct);

            if (existing is null)
            {
                existing = new TikTokTranscription { VideoId = info.Id };
                _db.TikTokTranscriptions.Add(existing);
            }

            existing.BeatModel = request.BeatModel;
            existing.ChordModel = request.ChordModel;
            existing.ChordDict = request.ChordDict;
            existing.BeatsJson = JsonSerializer.Serialize(result.Beats);
            existing.ChordsJson = JsonSerializer.Serialize(result.Chords);
            existing.SyncedChordsJson = JsonSerializer.Serialize(result.SyncedChords);
            existing.Bpm = result.Bpm;
            existing.TimeSignature = result.TimeSignature;
            existing.TotalProcessingSeconds = result.TotalProcessingSeconds;
            existing.CreatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
            result.Id = existing.Id;
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(YouTubeTranscriptionDto), 200)]
        public async Task<ActionResult<YouTubeTranscriptionDto>> GetTranscription(
            [FromQuery] string videoId,
            [FromQuery] string beatModel = "beat-transformer",
            [FromQuery] string chordModel = "chord-cnn-lstm",
            [FromQuery] string chordDict = "full")
        {
            var meta = await _db.TikTokVideos.AsNoTracking()
                .Where(v => v.VideoId == videoId)
                .Select(v => new { v.Title, v.ThumbnailUrl, v.Url, v.DurationSeconds })
                .FirstOrDefaultAsync();

            var (denied, access) = await GateAnalyzedAccessAsync(videoId, meta?.Title, meta?.ThumbnailUrl, meta?.Url, meta?.DurationSeconds);
            if (denied is not null)
                return denied;

            var t = await _db.TikTokTranscriptions.AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.VideoId == videoId &&
                    t.BeatModel == beatModel &&
                    t.ChordModel == chordModel &&
                    t.ChordDict == chordDict);

            if (t is null)
            {
                await ReleaseUnlockIfRecorded(access, videoId);
                return NotFound(new { message = $"No transcription found for '{videoId}'." });
            }

            return Ok(MapTranscriptionToDto(t));
        }

        private async Task<TikTokVideoDto> UpsertAndMapAsync(MediaInfo info, CancellationToken ct)
        {
            var existing = await _db.TikTokVideos.FirstOrDefaultAsync(v => v.VideoId == info.Id, ct);
            if (existing is null)
            {
                existing = new TikTokVideo
                {
                    VideoId = info.Id,
                    Url = info.WebpageUrl,
                    Title = Truncate(info.Title, 500),
                    Uploader = Truncate(info.Uploader, 255),
                    ThumbnailUrl = Truncate(info.Thumbnail, 1000),
                    DurationSeconds = info.DurationSeconds
                };
                _db.TikTokVideos.Add(existing);
                await _db.SaveChangesAsync(ct);
            }
            return MapToDto(existing);
        }

        private static bool IsTikTokUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var u) &&
            (u.Host.EndsWith("tiktok.com", StringComparison.OrdinalIgnoreCase));

        private static string? Truncate(string? s, int max) =>
            s is null ? null : s.Length <= max ? s : s[..max];

        private static TikTokVideoDto MapToDto(TikTokVideo v) => new()
        {
            VideoId = v.VideoId,
            Title = v.Title,
            Uploader = v.Uploader,
            ThumbnailUrl = v.ThumbnailUrl,
            DurationSeconds = v.DurationSeconds,
            Url = v.Url
        };

        private static YouTubeTranscriptionDto MapTranscriptionToDto(TikTokTranscription t) => new()
        {
            Id = t.Id,
            VideoId = t.VideoId,
            BeatModel = t.BeatModel,
            ChordModel = t.ChordModel,
            ChordDict = t.ChordDict,
            Beats = JsonSerializer.Deserialize<List<float>>(t.BeatsJson) ?? [],
            Chords = JsonSerializer.Deserialize<List<ChordEventDto>>(t.ChordsJson) ?? [],
            SyncedChords = JsonSerializer.Deserialize<List<SyncedChordDto>>(t.SyncedChordsJson) ?? [],
            Bpm = t.Bpm,
            TimeSignature = t.TimeSignature,
            KeySignature = t.KeySignature,
            TotalProcessingSeconds = t.TotalProcessingSeconds,
            CreatedAt = t.CreatedAt
        };
    }
}
