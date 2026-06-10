using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Services;
using FRELODYAPIs.Services.ChordMini;
using FRELODYSHRD.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class YouTubeController : ControllerBase
    {
        private readonly SongDbContext _db;
        private readonly YoutubeClient _youtube;
        private readonly IChordMiniService _chordMini;
        private readonly ISongService _songService;
        private readonly IAnalyzedAccessService _access;

        public YouTubeController(SongDbContext db, IChordMiniService chordMini, ISongService songService, IAnalyzedAccessService access)
        {
            _db = db;
            _youtube = new YoutubeClient();
            _chordMini = chordMini;
            _songService = songService;
            _access = access;
        }

        // Authoritative daily-quota / 24h-availability gate for analyzed playback.
        // Denied is a non-null IActionResult (402 + paywall payload, or an error) when
        // the caller must NOT receive the transcription; null when access is granted.
        // Access carries the evaluation so a downstream failure can refund a consumed
        // slot (see ReleaseUnlockIfRecorded).
        private async Task<(ActionResult<YouTubeTranscriptionDto>? Denied, AnalyzedAccessResultDto? Access)> GateAnalyzedAccessAsync(string videoId)
        {
            var meta = await _db.YouTubeVideos.AsNoTracking()
                .Where(v => v.VideoId == videoId)
                .Select(v => new { v.Title, v.ThumbnailUrl, v.DurationSeconds })
                .FirstOrDefaultAsync();

            var access = await _access.EvaluateAndRecord(
                AnalyzedPlatform.YouTube, videoId, meta?.Title, meta?.ThumbnailUrl,
                sourceUrl: null, durationSeconds: meta?.DurationSeconds);

            if (!access.IsSuccess)
                return (StatusCode(access.StatusCode, new { message = access.Error.Message }), null);

            // 402 Payment Required: in-context paywall (or sign-in) signal for the UI.
            if (!access.Data.Allowed)
                return (StatusCode(StatusCodes.Status402PaymentRequired, access.Data), access.Data);

            return (null, access.Data);
        }

        // Refund a freshly-consumed slot when we ultimately can't hand back chords,
        // so a failed analysis (bot-wall, timeout) doesn't burn the user's daily song.
        private async Task ReleaseUnlockIfRecorded(AnalyzedAccessResultDto? access, string videoId)
        {
            if (access?.Recorded == true)
                await _access.ReleaseUnlock(AnalyzedPlatform.YouTube, videoId);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<YouTubeVideoDto>), 200)]
        public async Task<ActionResult<List<YouTubeVideoDto>>> Search(
            [FromQuery] string q,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Query is required." });

            limit = Math.Clamp(limit, 1, 25);

            var results = new List<YouTubeVideoDto>();

            await foreach (var batch in _youtube.Search.GetResultBatchesAsync(q, SearchFilter.Video))
            {
                foreach (var result in batch.Items)
                {
                    if (result is not VideoSearchResult video)
                        continue;

                    var dto = await UpsertAndMapAsync(
                        video.Id.Value,
                        video.Title,
                        video.Author.ChannelTitle,
                        video.Thumbnails.GetWithHighestResolution()?.Url,
                        (int)(video.Duration?.TotalSeconds ?? 0));

                    results.Add(dto);

                    if (results.Count >= limit)
                        break;
                }

                if (results.Count >= limit)
                    break;
            }

            return Ok(results);
        }

        [HttpGet]
        [ProducesResponseType(typeof(YouTubeVideoDto), 200)]
        public async Task<ActionResult<YouTubeVideoDto>> GetVideo([FromQuery] string videoId)
        {
            var cached = await _db.YouTubeVideos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VideoId == videoId);

            if (cached is not null)
                return Ok(MapToDto(cached));

            try
            {
                var video = await _youtube.Videos.GetAsync(videoId);
                var dto = await UpsertAndMapAsync(
                    video.Id.Value,
                    video.Title,
                    video.Author.ChannelTitle,
                    video.Thumbnails.GetWithHighestResolution()?.Url,
                    (int)video.Duration.GetValueOrDefault().TotalSeconds);

                return Ok(dto);
            }
            catch
            {
                return NotFound(new { message = $"Video '{videoId}' not found." });
            }
        }

        [HttpPost]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("analysis")]
        [ProducesResponseType(typeof(YouTubeTranscriptionDto), 200)]
        public async Task<ActionResult<YouTubeTranscriptionDto>> Analyze(
            [FromBody] YouTubeAnalyzeRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.VideoId))
                return BadRequest(new { message = "videoId is required." });

            // Meter the analyzed play before returning any chord data (cached or fresh).
            var (denied, access) = await GateAnalyzedAccessAsync(request.VideoId);
            if (denied is not null)
                return denied;

            // Return cached result when not forcing a refresh
            if (!request.ForceRefresh)
            {
                var cached = await _db.YouTubeTranscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.VideoId == request.VideoId &&
                        t.BeatModel == request.BeatModel &&
                        t.ChordModel == request.ChordModel &&
                        t.ChordDict == request.ChordDict,
                        ct);

                if (cached is not null)
                    return Ok(MapTranscriptionToDto(cached));
            }

            // Run full analysis via ChordMini sidecar
            YouTubeTranscriptionDto result;
            try
            {
                result = await _chordMini.AnalyzeAsync(request, ct);
            }
            catch (Exception ex)
            {
                // Analysis failed (bot-wall, timeout, …) — refund the slot we just
                // consumed so the user keeps their daily song for another try.
                await ReleaseUnlockIfRecorded(access, request.VideoId);
                // Return 422 (Unprocessable Entity) for known analysis failures
                // to avoid triggering the generic Cloudflare/Nginx 502 error page.
                return StatusCode(422, new { message = ex.Message });
            }

            // Ensure the parent YouTubeVideo row exists (FK requirement)
            var videoExists = await _db.YouTubeVideos.AnyAsync(v => v.VideoId == request.VideoId, ct);
            if (!videoExists)
            {
                try
                {
                    var video = await _youtube.Videos.GetAsync(request.VideoId, ct);
                    await UpsertAndMapAsync(
                        video.Id.Value,
                        video.Title,
                        video.Author.ChannelTitle,
                        video.Thumbnails.GetWithHighestResolution()?.Url,
                        (int)video.Duration.GetValueOrDefault().TotalSeconds);
                }
                catch
                {
                    // Fallback: insert a minimal placeholder so the FK is satisfied
                    _db.YouTubeVideos.Add(new YouTubeVideo { VideoId = request.VideoId, Title = request.VideoId });
                    await _db.SaveChangesAsync(ct);
                }
            }

            // Persist result (upsert by unique index)
            var existing = await _db.YouTubeTranscriptions.FirstOrDefaultAsync(t =>
                t.VideoId == request.VideoId &&
                t.BeatModel == request.BeatModel &&
                t.ChordModel == request.ChordModel &&
                t.ChordDict == request.ChordDict, ct);

            if (existing is null)
            {
                existing = new YouTubeTranscription { VideoId = request.VideoId };
                _db.YouTubeTranscriptions.Add(existing);
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
            var (denied, access) = await GateAnalyzedAccessAsync(videoId);
            if (denied is not null)
                return denied;

            var t = await _db.YouTubeTranscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.VideoId == videoId &&
                    t.BeatModel == beatModel &&
                    t.ChordModel == chordModel &&
                    t.ChordDict == chordDict);

            if (t is null)
            {
                // Nothing to hand back — don't let the gate's slot stick.
                await ReleaseUnlockIfRecorded(access, videoId);
                return NotFound(new { message = $"No transcription found for '{videoId}'." });
            }

            return Ok(MapTranscriptionToDto(t));
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(SongDto), 200)]
        public async Task<ActionResult<SongDto>> SaveToLibrary(
            [FromBody] YouTubeSaveRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.VideoId))
                return BadRequest(new { message = "videoId is required." });

            var transcription = await _db.YouTubeTranscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.VideoId == request.VideoId &&
                    t.BeatModel == request.BeatModel &&
                    t.ChordModel == request.ChordModel &&
                    t.ChordDict == request.ChordDict,
                    ct);

            if (transcription is null)
                return NotFound(new { message = "No analysis found for this video. Analyze it first." });

            var dto = MapTranscriptionToDto(transcription);
            if (dto.SyncedChords.Count == 0)
                return BadRequest(new { message = "This analysis has no chords to save." });

            var video = await _db.YouTubeVideos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VideoId == request.VideoId, ct);

            var title = !string.IsNullOrWhiteSpace(request.Title)
                ? request.Title!.Trim()
                : video?.Title ?? request.VideoId;

            // Chords-only charts are hard to follow — enrich the save with LRCLib synced
            // lyrics when a confident match exists. Strictly best-effort: any miss falls
            // back to the chord-only chart.
            IReadOnlyList<LyricsLine>? lyricLines = null;
            try
            {
                var (artist, songTitle) = YouTubeSongMapper.ParseArtistTitle(title);
                var lyrics = await _chordMini.GetLyricsAsync(
                    artist ?? video?.ChannelTitle, songTitle, searchQuery: title, ct);

                // Wrong-song guard: when both durations are known they must roughly agree.
                var durationOk = lyrics.DurationSeconds is not double d ||
                                 video?.DurationSeconds is not > 0 ||
                                 Math.Abs(d - video.DurationSeconds) <= Math.Max(20, video.DurationSeconds * 0.15);

                if (lyrics.Found && lyrics.HasSynchronized && durationOk)
                    lyricLines = lyrics.Synchronized;
            }
            catch
            {
                // Lyrics enrichment must never block saving the chart.
            }

            var songDto = YouTubeSongMapper.Map(dto, title, lyricLines);

            var result = await _songService.CreateSong(songDto);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        private async Task<YouTubeVideoDto> UpsertAndMapAsync(
            string videoId, string title, string? channel, string? thumbnail, int durationSeconds)
        {
            var existing = await _db.YouTubeVideos.FirstOrDefaultAsync(v => v.VideoId == videoId);
            if (existing is null)
            {
                existing = new YouTubeVideo
                {
                    VideoId = videoId,
                    Title = title,
                    ChannelTitle = channel,
                    ThumbnailUrl = thumbnail,
                    DurationSeconds = durationSeconds
                };
                _db.YouTubeVideos.Add(existing);
                await _db.SaveChangesAsync();
            }

            return MapToDto(existing);
        }

        private static YouTubeVideoDto MapToDto(YouTubeVideo v) => new()
        {
            VideoId = v.VideoId,
            Title = v.Title,
            ChannelTitle = v.ChannelTitle,
            ThumbnailUrl = v.ThumbnailUrl,
            DurationSeconds = v.DurationSeconds
        };

        private static YouTubeTranscriptionDto MapTranscriptionToDto(YouTubeTranscription t) => new()
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
