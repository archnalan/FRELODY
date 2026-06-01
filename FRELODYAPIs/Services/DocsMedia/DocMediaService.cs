using System.Text.Json;
using System.Text.RegularExpressions;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.DocsDtos;

namespace FRELODYAPIs.Services.DocsMedia
{
    public sealed class DocMediaService : IDocMediaService
    {
        // Slot keys are lowercase alphanumerics joined by dashes (incl. the "--" separator
        // used by the registry, e.g. "discover-overview--1"). This also blocks path traversal
        // because '/', '\\' and '.' can never appear.
        private static readonly Regex SlotPattern = new(@"^[a-z0-9]+(?:-+[a-z0-9]+)*$", RegexOptions.Compiled);

        // Bare 11-char id, or any of the common YouTube URL shapes.
        private static readonly Regex BareYouTubeId = new(@"^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);
        private static readonly Regex YouTubeUrlId = new(
            @"(?:youtu\.be/|youtube(?:-nocookie)?\.com/(?:watch\?(?:[^#]*&)?v=|embed/|shorts/|v/))([A-Za-z0-9_-]{11})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly Dictionary<string, string> ContentTypeToExt = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/jpg"] = ".jpg",
            ["image/webp"] = ".webp",
        };
        private static readonly string[] KnownExts = { ".png", ".jpg", ".jpeg", ".webp" };

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        // One writer at a time: the manifest is a single file (read-modify-write).
        private static readonly SemaphoreSlim Gate = new(1, 1);

        private readonly string _root;
        private readonly string _manifestPath;
        private readonly ILogger<DocMediaService> _logger;

        public DocMediaService(IWebHostEnvironment env, IConfiguration config, ILogger<DocMediaService> logger)
        {
            _logger = logger;
            // Default "media/docs-media" resolves to /app/media/docs-media in Docker, which is the
            // mounted frelody_media volume (persists across redeploys).
            var rel = config["DocsMedia:Root"] ?? Path.Combine("media", "docs-media");
            _root = Path.IsPathRooted(rel) ? rel : Path.Combine(env.ContentRootPath, rel);
            _manifestPath = Path.Combine(_root, "manifest.json");
            Directory.CreateDirectory(_root);
        }

        public async Task<ServiceResult<DocMediaManifestDto>> GetManifestAsync(CancellationToken ct = default)
        {
            try
            {
                var manifest = await ReadManifestAsync(ct);
                return ServiceResult<DocMediaManifestDto>.Success(manifest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read docs media manifest");
                return ServiceResult<DocMediaManifestDto>.Failure(new ServerErrorException("Could not read media manifest."));
            }
        }

        public async Task<ServiceResult<DocMediaEntryDto>> SaveImageAsync(string slot, IFormFile file, CancellationToken ct = default)
        {
            if (!IsValidSlot(slot))
                return ServiceResult<DocMediaEntryDto>.Failure(new BadRequestException("Invalid slot key."));
            if (file is null || file.Length == 0)
                return ServiceResult<DocMediaEntryDto>.Failure(new BadRequestException("No file uploaded."));
            if (file.Length > MaxImageBytes)
                return ServiceResult<DocMediaEntryDto>.Failure(new BadRequestException("Image exceeds the 5 MB limit."));
            if (!ContentTypeToExt.TryGetValue(file.ContentType ?? string.Empty, out var ext))
                return ServiceResult<DocMediaEntryDto>.Failure(new BadRequestException("Unsupported image type. Use PNG, JPEG or WebP."));

            await Gate.WaitAsync(ct);
            try
            {
                // Remove any prior image for this slot (possibly a different extension).
                foreach (var e in KnownExts)
                {
                    var prior = Path.Combine(_root, slot + e);
                    if (File.Exists(prior)) File.Delete(prior);
                }

                var fileName = slot + ext;
                var fullPath = Path.Combine(_root, fileName);
                await using (var fs = File.Create(fullPath))
                {
                    await file.CopyToAsync(fs, ct);
                }

                var manifest = await ReadManifestUnlockedAsync(ct);
                var entry = GetOrCreate(manifest, slot);
                entry.ImageUrl = $"/docs-media/{fileName}";
                entry.UpdatedAt = DateTimeOffset.UtcNow;
                await WriteManifestAsync(manifest, ct);

                return ServiceResult<DocMediaEntryDto>.Success(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save docs media image for slot {Slot}", slot);
                return ServiceResult<DocMediaEntryDto>.Failure(new ServerErrorException("Could not save the image."));
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<ServiceResult<DocMediaEntryDto>> SetTextAsync(string slot, DocMediaTextUpdateDto dto, CancellationToken ct = default)
        {
            if (!IsValidSlot(slot))
                return ServiceResult<DocMediaEntryDto>.Failure(new BadRequestException("Invalid slot key."));

            string? videoId = null;
            if (!string.IsNullOrWhiteSpace(dto?.VideoUrlOrId))
            {
                videoId = ExtractYouTubeId(dto!.VideoUrlOrId!);
                if (videoId is null)
                    return ServiceResult<DocMediaEntryDto>.Failure(
                        new BadRequestException("Could not read a YouTube video id from that link."));
            }

            await Gate.WaitAsync(ct);
            try
            {
                var manifest = await ReadManifestUnlockedAsync(ct);
                var entry = GetOrCreate(manifest, slot);
                entry.VideoId = videoId; // null clears
                entry.Caption = string.IsNullOrWhiteSpace(dto?.Caption) ? null : dto!.Caption!.Trim();
                entry.UpdatedAt = DateTimeOffset.UtcNow;
                await WriteManifestAsync(manifest, ct);
                return ServiceResult<DocMediaEntryDto>.Success(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set docs media text for slot {Slot}", slot);
                return ServiceResult<DocMediaEntryDto>.Failure(new ServerErrorException("Could not save the video details."));
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<ServiceResult<DocMediaEntryDto>> ClearAsync(string slot, string kind, CancellationToken ct = default)
        {
            if (!IsValidSlot(slot))
                return ServiceResult<DocMediaEntryDto>.Failure(new BadRequestException("Invalid slot key."));
            kind = (kind ?? string.Empty).Trim().ToLowerInvariant();
            if (kind != "image" && kind != "video")
                return ServiceResult<DocMediaEntryDto>.Failure(new BadRequestException("kind must be 'image' or 'video'."));

            await Gate.WaitAsync(ct);
            try
            {
                var manifest = await ReadManifestUnlockedAsync(ct);
                var entry = GetOrCreate(manifest, slot);

                if (kind == "image")
                {
                    foreach (var e in KnownExts)
                    {
                        var prior = Path.Combine(_root, slot + e);
                        if (File.Exists(prior)) File.Delete(prior);
                    }
                    entry.ImageUrl = null;
                }
                else
                {
                    entry.VideoId = null;
                }

                entry.UpdatedAt = DateTimeOffset.UtcNow;
                await WriteManifestAsync(manifest, ct);
                return ServiceResult<DocMediaEntryDto>.Success(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear docs media {Kind} for slot {Slot}", kind, slot);
                return ServiceResult<DocMediaEntryDto>.Failure(new ServerErrorException("Could not clear the media."));
            }
            finally
            {
                Gate.Release();
            }
        }

        // ── helpers ────────────────────────────────────────────────────────────

        private static bool IsValidSlot(string? slot) =>
            !string.IsNullOrWhiteSpace(slot) && slot.Length <= 100 && SlotPattern.IsMatch(slot);

        internal static string? ExtractYouTubeId(string input)
        {
            input = input.Trim();
            if (BareYouTubeId.IsMatch(input)) return input;
            var m = YouTubeUrlId.Match(input);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static DocMediaEntryDto GetOrCreate(DocMediaManifestDto manifest, string slot)
        {
            if (!manifest.Slots.TryGetValue(slot, out var entry))
            {
                entry = new DocMediaEntryDto { SlotKey = slot };
                manifest.Slots[slot] = entry;
            }
            entry.SlotKey = slot;
            return entry;
        }

        private Task<DocMediaManifestDto> ReadManifestAsync(CancellationToken ct) => ReadManifestUnlockedAsync(ct);

        private async Task<DocMediaManifestDto> ReadManifestUnlockedAsync(CancellationToken ct)
        {
            if (!File.Exists(_manifestPath)) return new DocMediaManifestDto();
            await using var fs = File.OpenRead(_manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync<DocMediaManifestDto>(fs, JsonOpts, ct);
            return manifest ?? new DocMediaManifestDto();
        }

        private async Task WriteManifestAsync(DocMediaManifestDto manifest, CancellationToken ct)
        {
            // Write to a temp file then atomically replace, so concurrent readers never see a
            // half-written manifest.
            var tmp = _manifestPath + ".tmp";
            await using (var fs = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(fs, manifest, JsonOpts, ct);
            }
            File.Move(tmp, _manifestPath, overwrite: true);
        }
    }
}
