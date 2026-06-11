using System.Text.Json;
using System.Text.RegularExpressions;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.YoutubeCookieDtos;

namespace FRELODYAPIs.Services.YoutubeCookies
{
    public sealed class YoutubeCookieService : IYoutubeCookieService
    {
        // Valid slot label slugs: lowercase alphanumerics separated by dashes.
        // Same pattern as DocMediaService slot keys — also blocks path traversal.
        private static readonly Regex LabelPattern = new(@"^[a-z0-9]+(?:-+[a-z0-9]+)*$", RegexOptions.Compiled);

        // Auth cookies that signal a logged-in YouTube session — matches cookie_refresher.py AUTH_COOKIE_NAMES.
        private static readonly HashSet<string> AuthCookieNames = new(StringComparer.Ordinal)
        {
            "LOGIN_INFO", "SID", "__Secure-1PSID", "__Secure-3PSID"
        };

        // One writer at a time: slot pruning + atomic write must be serialised.
        private static readonly SemaphoreSlim Gate = new(1, 1);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        private readonly string _seedsRoot;
        private readonly string _statusFile;
        private readonly int _maxSlots;
        private readonly ILogger<YoutubeCookieService> _logger;

        public YoutubeCookieService(IWebHostEnvironment env, IConfiguration config, ILogger<YoutubeCookieService> logger)
        {
            _logger = logger;

            var seedsRel = config["YoutubeCookies:SeedsRoot"] ?? Path.Combine("media", "cookie-seeds");
            _seedsRoot = Path.IsPathRooted(seedsRel) ? seedsRel : Path.Combine(env.ContentRootPath, seedsRel);

            var statusRel = config["YoutubeCookies:StatusFile"] ?? Path.Combine("media", "cookie-status.json");
            _statusFile = Path.IsPathRooted(statusRel) ? statusRel : Path.Combine(env.ContentRootPath, statusRel);

            _maxSlots = int.TryParse(config["YoutubeCookies:MaxSlots"], out var ms) ? ms : 3;

            Directory.CreateDirectory(_seedsRoot);
        }

        public async Task<ServiceResult<CookieStatusDto>> GetStatusAsync(CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(_statusFile))
                    return ServiceResult<CookieStatusDto>.Success(new CookieStatusDto
                    {
                        Published = false,
                        Note = "status not yet published",
                        Slots = new List<CookieSlotDto>(),
                    });

                await using var fs = File.OpenRead(_statusFile);
                var dto = await JsonSerializer.DeserializeAsync<CookieStatusDto>(fs, JsonOpts, ct);
                if (dto is null)
                    return ServiceResult<CookieStatusDto>.Failure(new ServerErrorException("Could not read cookie status."));
                dto.Published = true;
                return ServiceResult<CookieStatusDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read cookie-status.json");
                return ServiceResult<CookieStatusDto>.Failure(new ServerErrorException("Could not read cookie status."));
            }
        }

        public async Task<ServiceResult<SaveCookiesResultDto>> SaveCookiesAsync(SaveCookiesRequestDto req, CancellationToken ct = default)
        {
            // Validate the pasted Netscape text before touching the filesystem.
            var (cookieCount, authFound, validationError) = ValidateNetscape(req.CookieText);
            if (validationError is not null)
                return ServiceResult<SaveCookiesResultDto>.Failure(new BadRequestException(validationError));

            await Gate.WaitAsync(ct);
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var slug = SanitizeLabel(req.Label);
                var slotName = slug is not null
                    ? $"youtube-{timestamp}-{slug}.seed.txt"
                    : $"youtube-{timestamp}.seed.txt";

                var dest = Path.Combine(_seedsRoot, slotName);
                AtomicWrite(dest, req.CookieText);

                // Prune: keep the newest MaxSlots, delete the rest.
                PruneSlots();

                var remaining = CountSlots();
                return ServiceResult<SaveCookiesResultDto>.Success(new SaveCookiesResultDto
                {
                    SlotName = slotName,
                    CookieCount = cookieCount,
                    HasAuthCookies = authFound.Count > 0,
                    AuthCookiesFound = authFound,
                    SlotCount = remaining,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write cookie slot");
                return ServiceResult<SaveCookiesResultDto>.Failure(new ServerErrorException("Could not save the cookie slot."));
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<ServiceResult<List<CookieSlotDto>>> DeleteSlotAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<List<CookieSlotDto>>.Failure(new BadRequestException("Slot name is required."));

            // name must be a bare filename — no directory separators or traversal sequences.
            if (Path.GetFileName(name) != name || name.Contains(".."))
                return ServiceResult<List<CookieSlotDto>>.Failure(new BadRequestException("Invalid slot name."));

            var fullPath = Path.GetFullPath(Path.Combine(_seedsRoot, name));
            if (!fullPath.StartsWith(Path.GetFullPath(_seedsRoot), StringComparison.OrdinalIgnoreCase))
                return ServiceResult<List<CookieSlotDto>>.Failure(new BadRequestException("Invalid slot name."));

            await Gate.WaitAsync(ct);
            try
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                return ServiceResult<List<CookieSlotDto>>.Success(ListSlots());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cookie slot {Name}", name);
                return ServiceResult<List<CookieSlotDto>>.Failure(new ServerErrorException("Could not delete the slot."));
            }
            finally
            {
                Gate.Release();
            }
        }

        // ── helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses a Netscape cookie export, matching cookie_refresher.py's parse_netscape().
        /// Returns (validCookieCount, authNamesFound, errorMessage).
        /// </summary>
        private static (int count, List<string> authFound, string? error) ValidateNetscape(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (0, new(), "That doesn't look like a Netscape cookies.txt export.");

            var names = new List<string>();
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (string.IsNullOrEmpty(line)) continue;

                // Skip comments, but honour #HttpOnly_ prefix (same logic as refresher).
                if (line.StartsWith('#') && !line.StartsWith("#HttpOnly_")) continue;
                if (line.StartsWith("#HttpOnly_")) line = line["#HttpOnly_".Length..];

                var parts = line.Split('\t');
                if (parts.Length != 7) continue;

                names.Add(parts[5]); // name is field index 5
            }

            if (names.Count == 0)
                return (0, new(), "That doesn't look like a Netscape cookies.txt export.");

            var authFound = names.Intersect(AuthCookieNames).ToList();
            if (authFound.Count == 0)
                return (names.Count, new(), "No logged-in session cookies found — export while signed in to YouTube.");

            return (names.Count, authFound, null);
        }

        private static string? SanitizeLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label)) return null;
            var slug = label.Trim().ToLowerInvariant()
                            .Replace(' ', '-')
                            .Replace('_', '-');
            // Strip anything not alphanumeric or dash.
            slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
            slug = Regex.Replace(slug, @"-{2,}", "-").Trim('-');
            if (slug.Length == 0 || slug.Length > 40) return null;
            return LabelPattern.IsMatch(slug) ? slug : null;
        }

        private static void AtomicWrite(string dest, string content)
        {
            var dir = Path.GetDirectoryName(dest) ?? ".";
            Directory.CreateDirectory(dir);
            var tmp = dest + ".tmp";
            File.WriteAllText(tmp, content, System.Text.Encoding.UTF8);
            // 0644: owner rw, group r, other r — chordmini's non-root user (uid 1001) can read.
            File.SetUnixFileMode(tmp,
                UnixFileMode.UserRead | UnixFileMode.UserWrite |
                UnixFileMode.GroupRead |
                UnixFileMode.OtherRead);
            File.Move(tmp, dest, overwrite: true);
        }

        private void PruneSlots()
        {
            var slots = Directory.GetFiles(_seedsRoot, "*.seed.txt")
                                  .OrderByDescending(File.GetLastWriteTimeUtc)
                                  .ToList();
            foreach (var old in slots.Skip(_maxSlots))
            {
                try { File.Delete(old); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not prune slot {Path}", old); }
            }
        }

        private int CountSlots() =>
            Directory.GetFiles(_seedsRoot, "*.seed.txt").Length;

        private List<CookieSlotDto> ListSlots() =>
            Directory.GetFiles(_seedsRoot, "*.seed.txt")
                     .Select(p => new CookieSlotDto
                     {
                         Name = Path.GetFileName(p),
                         UpdatedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(p), TimeSpan.Zero),
                     })
                     .OrderByDescending(s => s.UpdatedAt)
                     .ToList();
    }
}
