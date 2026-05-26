using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;

namespace FRELODYAPIs.Controllers
{
    [Route("api/youtube")]
    [ApiController]
    public class YouTubeController : ControllerBase
    {
        private readonly SongDbContext _db;
        private readonly YoutubeClient _youtube;

        public YouTubeController(SongDbContext db)
        {
            _db = db;
            _youtube = new YoutubeClient();
        }

        [HttpGet("search")]
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

        [HttpGet("videos/{videoId}")]
        [ProducesResponseType(typeof(YouTubeVideoDto), 200)]
        public async Task<ActionResult<YouTubeVideoDto>> GetVideo(string videoId)
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
    }
}
