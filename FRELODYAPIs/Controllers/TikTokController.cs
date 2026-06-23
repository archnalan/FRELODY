using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Services.Analysis;
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
        private readonly AnalysisJobService _jobs;

        public TikTokController(SongDbContext db, IChordMiniService chordMini, IAnalyzedAccessService access, AnalysisJobService jobs)
        {
            _db = db;
            _chordMini = chordMini;
            _access = access;
            _jobs = jobs;
        }

        // Authoritative daily-quota / 24h-availability gate for analyzed playback.
        // Denied is a non-null IActionResult (402 + paywall payload, or an error) when
        // the caller must NOT receive the transcription; null when access is granted.
        // Access carries the evaluation so a downstream failure can refund a consumed slot.
        private async Task<(ActionResult? Denied, AnalyzedAccessResultDto? Access)> GateAnalyzedAccessAsync(string videoId, string? title, string? thumbnailUrl, string? sourceUrl, int? durationSeconds)
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
                // App-level resolve failure → 422 so the UI shows the friendly message
                // instead of a Cloudflare/nginx 502 page. Mirrors YouTubeController.
                return StatusCode(422, new { message = ex.Message });
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
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("analysis")]
        [ProducesResponseType(typeof(AnalysisStatusDto), 200)]
        public async Task<ActionResult<AnalysisStatusDto>> Analyze(
            [FromBody] TikTokAnalyzeRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Url) || !IsTikTokUrl(request.Url))
                return BadRequest(new { message = "A valid TikTok URL is required." });

            // Anonymous users don't need a (costly, bot-wall-prone) resolve to be told to
            // sign in — short-circuit to the same 402 nudge YouTube shows, before any
            // network call. The placeholder id is never persisted on the anon path.
            if (User.Identity?.IsAuthenticated != true)
            {
                var (anonDenied, _) = await GateAnalyzedAccessAsync(
                    request.Url, null, null, request.Url, null);
                if (anonDenied is not null)
                    return anonDenied;
            }

            // Resolve id + metadata first so we can cache by id.
            MediaInfo info;
            try
            {
                info = await _chordMini.GetInfoAsync(request.Url, ct);
            }
            catch (Exception ex)
            {
                // App-level failure (region/copyright/bot-wall) — 422 not 502 so
                // Cloudflare/nginx don't swap in a generic Bad Gateway HTML page and
                // the UI can surface the friendly message. Mirrors YouTubeController.
                return StatusCode(422, new { message = ex.Message });
            }

            // Meter the analyzed play before returning any chord data (cached or fresh).
            var (denied, _) = await GateAnalyzedAccessAsync(info.Id, Truncate(info.Title, 500), Truncate(info.Thumbnail, 1000), info.WebpageUrl, info.DurationSeconds);
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
                    return Ok(new AnalysisStatusDto
                    {
                        Stage = AnalysisStage.Done,
                        VideoId = info.Id,
                        Result = MapTranscriptionToDto(cached)
                    });
            }

            // Cache miss: run on a background task decoupled from this HTTP request (immune to
            // client disconnect / Cloudflare ~100s edge cut). The client polls get-analysis-status
            // and fetches the result via get-transcription when Done.
            var key = AnalysisJobService.BuildKey(
                "tiktok", info.Id, request.BeatModel, request.ChordModel, request.ChordDict);

            var req = request;
            var media = info;
            var job = _jobs.Submit(key, async (sp, progress, token) =>
            {
                var chord = sp.GetRequiredService<IChordMiniService>();
                var db = sp.GetRequiredService<SongDbContext>();

                var result = await chord.AnalyzeUrlAsync(
                    media.WebpageUrl, media.Id,
                    req.BeatModel, req.ChordModel, req.ChordDict, progress, token);

                // Ensure the parent TikTokVideo row exists (FK requirement).
                var videoExists = await db.TikTokVideos.AnyAsync(v => v.VideoId == media.Id, token);
                if (!videoExists)
                {
                    db.TikTokVideos.Add(new TikTokVideo
                    {
                        VideoId = media.Id,
                        Url = media.WebpageUrl,
                        Title = Truncate(media.Title, 500),
                        Uploader = Truncate(media.Uploader, 255),
                        ThumbnailUrl = Truncate(media.Thumbnail, 1000),
                        DurationSeconds = media.DurationSeconds
                    });
                    await db.SaveChangesAsync(token);
                }

                var existing = await db.TikTokTranscriptions.FirstOrDefaultAsync(t =>
                    t.VideoId == media.Id &&
                    t.BeatModel == req.BeatModel &&
                    t.ChordModel == req.ChordModel &&
                    t.ChordDict == req.ChordDict, token);

                if (existing is null)
                {
                    existing = new TikTokTranscription { VideoId = media.Id };
                    db.TikTokTranscriptions.Add(existing);
                }

                existing.BeatModel = req.BeatModel;
                existing.ChordModel = req.ChordModel;
                existing.ChordDict = req.ChordDict;
                existing.BeatsJson = JsonSerializer.Serialize(result.Beats);
                existing.ChordsJson = JsonSerializer.Serialize(result.Chords);
                existing.SyncedChordsJson = JsonSerializer.Serialize(result.SyncedChords);
                existing.Bpm = result.Bpm;
                existing.TimeSignature = result.TimeSignature;
                existing.TotalProcessingSeconds = result.TotalProcessingSeconds;
                existing.CreatedAt = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync(token);
            });

            return Ok(new AnalysisStatusDto { Stage = job.Stage, VideoId = info.Id });
        }

        // Lightweight poll for an in-flight (or just-finished) TikTok analysis — see the
        // YouTube equivalent. Returns the live stage; the gated get-transcription hands back
        // the chords once Done, so the long run is driven by short, timeout-proof requests.
        [HttpGet]
        [ProducesResponseType(typeof(AnalysisStatusDto), 200)]
        public async Task<ActionResult<AnalysisStatusDto>> GetAnalysisStatus(
            [FromQuery] string videoId,
            [FromQuery] string beatModel = "beat-transformer",
            [FromQuery] string chordModel = "chord-cnn-lstm",
            [FromQuery] string chordDict = "full")
        {
            if (string.IsNullOrWhiteSpace(videoId))
                return BadRequest(new { message = "videoId is required." });

            var key = AnalysisJobService.BuildKey("tiktok", videoId, beatModel, chordModel, chordDict);
            var job = _jobs.Get(key);

            if (job is null || job.Stage == AnalysisStage.Done)
            {
                var done = await _db.TikTokTranscriptions.AsNoTracking().AnyAsync(t =>
                    t.VideoId == videoId && t.BeatModel == beatModel &&
                    t.ChordModel == chordModel && t.ChordDict == chordDict);

                if (done)
                    return Ok(new AnalysisStatusDto { Stage = AnalysisStage.Done, VideoId = videoId });

                if (job is null)
                    return Ok(new AnalysisStatusDto { Stage = AnalysisStage.NotStarted, VideoId = videoId });
            }

            if (job!.Stage == AnalysisStage.Failed)
            {
                await _access.ReleaseUnlock(AnalyzedPlatform.TikTok, videoId);
                return Ok(new AnalysisStatusDto { Stage = AnalysisStage.Failed, VideoId = videoId, Error = job.Error });
            }

            return Ok(new AnalysisStatusDto
            {
                Stage = job.Stage,
                VideoId = videoId,
                QueueAhead = job.Stage == AnalysisStage.Queued ? _jobs.QueueAhead(job) : null
            });
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
