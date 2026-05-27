using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYAPIs.Services.ChordMini;
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

        public YouTubeController(SongDbContext db, IChordMiniService chordMini)
        {
            _db = db;
            _youtube = new YoutubeClient();
            _chordMini = chordMini;
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
        [ProducesResponseType(typeof(YouTubeTranscriptionDto), 200)]
        public async Task<ActionResult<YouTubeTranscriptionDto>> Analyze(
            [FromBody] YouTubeAnalyzeRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.VideoId))
                return BadRequest(new { message = "videoId is required." });

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
            var t = await _db.YouTubeTranscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.VideoId == videoId &&
                    t.BeatModel == beatModel &&
                    t.ChordModel == chordModel &&
                    t.ChordDict == chordDict);

            if (t is null)
                return NotFound(new { message = $"No transcription found for '{videoId}'." });

            return Ok(MapTranscriptionToDto(t));
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
