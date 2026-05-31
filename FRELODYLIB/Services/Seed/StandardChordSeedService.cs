using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYAPP.Services.ChordDraw;
using FRELODYSHRD.Models.ChordDraw;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FRELODYAPP.Services.Seed
{
    public interface IStandardChordSeedService
    {
        Task<SeedResult> SeedIfNeededAsync(bool force = false, CancellationToken cancellationToken = default);
    }

    public record SeedResult(bool Ran, int ChordsInserted, int VoicingsSeeded, int DuplicatesMerged, string Version, string? SkipReason = null);

    public class StandardChordSeedService : IStandardChordSeedService
    {
        private const string SeedKey = "standard-chords";
        private const string SeedActor = "system-seed";
        private static readonly string[] SourceRelativePath = ["seed-chords", "_source", "chords-db.guitar.json"];

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly SongDbContext _db;
        private readonly IHostEnvironment _env;
        private readonly ChordSvgRenderer _renderer;
        private readonly ILogger<StandardChordSeedService> _logger;

        public StandardChordSeedService(
            SongDbContext db,
            IHostEnvironment env,
            ChordSvgRenderer renderer,
            ILogger<StandardChordSeedService> logger)
        {
            _db = db;
            _env = env;
            _renderer = renderer;
            _logger = logger;
        }

        public async Task<SeedResult> SeedIfNeededAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            var sourcePath = ResolveSourcePath();
            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Standard chord source not found at {Path}; skipping seed.", sourcePath);
                return new SeedResult(false, 0, 0, 0, "", "source-missing");
            }

            var hash = ComputeFileHash(sourcePath);

            var version = await _db.SeedVersions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.SeedName == SeedKey, cancellationToken);

            if (!force && version?.Version == hash)
            {
                _logger.LogDebug("Standard chord seed already current at {Version}.", hash);
                return new SeedResult(false, 0, 0, 0, hash, "up-to-date");
            }

            var importer = new ChordsDbImporter();
            var voicings = importer.Import(sourcePath);
            if (voicings.Count == 0)
            {
                _logger.LogWarning("Chord importer returned zero voicings; aborting seed.");
                return new SeedResult(false, 0, 0, 0, hash, "no-voicings");
            }

            using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Collapse pre-existing duplicate chord-name rows first. This both repairs the
            // catalog (so the upserts below can key safely by name) and is required because
            // ad-hoc chord creation during song import does not de-dup by name.
            var duplicatesMerged = await MergeDuplicateChordsAsync(cancellationToken);
            var chordsInserted = await UpsertChordsAsync(voicings, cancellationToken);
            var voicingsSeeded = await ReplaceStandardChartsAsync(voicings, cancellationToken);
            await UpsertSeedVersionAsync(version, hash, cancellationToken);

            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Standard chord seed complete: +{ChordsInserted} chords, {Voicings} voicings, {Merged} duplicates merged, version {Version}.",
                chordsInserted, voicingsSeeded, duplicatesMerged, hash);

            return new SeedResult(true, chordsInserted, voicingsSeeded, duplicatesMerged, hash);
        }

        /// <summary>
        /// Collapses duplicate chord-name rows into a single canonical chord, preserving every
        /// user-created artifact: all <see cref="ChordChart"/> and <see cref="LyricSegment"/>
        /// references on the duplicates are repointed onto the survivor, and any metadata the
        /// user set on a duplicate is coalesced onto it. Only the redundant name rows are deleted —
        /// no chart a user drew is ever lost.
        /// </summary>
        private async Task<int> MergeDuplicateChordsAsync(CancellationToken ct)
        {
            // Live rows only — leave soft-deleted chords untouched.
            var liveChords = await _db.Chords
                .IgnoreQueryFilters()
                .Where(c => c.IsDeleted == false || c.IsDeleted == null)
                .ToListAsync(ct);

            // Group on the trimmed name so whitespace-only variants ("Eb" vs "Eb ")
            // collapse too — those are invisible to an exact-match lookup but real duplicates.
            var dupeGroups = liveChords
                .GroupBy(c => c.ChordName.Trim())
                .Where(g => g.Count() > 1)
                .ToList();

            if (dupeGroups.Count == 0) return 0;

            var mergedCount = 0;
            foreach (var group in dupeGroups)
            {
                // Survivor: prefer the global/standard row (TenantId == null), then the oldest.
                var ordered = group
                    .OrderByDescending(c => c.TenantId == null)
                    .ThenBy(c => c.DateCreated)
                    .ToList();
                var canonical = ordered[0];
                var duplicates = ordered.Skip(1).ToList();
                var duplicateIds = duplicates.Select(c => c.Id).ToList();

                // Normalize the survivor's name to the trimmed form so lookups match.
                canonical.ChordName = group.Key;

                // Keep metadata a user may have set on a duplicate but not on the survivor.
                canonical.Difficulty ??= duplicates.FirstOrDefault(d => d.Difficulty != null)?.Difficulty;
                canonical.ChordType ??= duplicates.FirstOrDefault(d => d.ChordType != null)?.ChordType;
                canonical.ChordAudioFilePath ??= duplicates
                    .FirstOrDefault(d => !string.IsNullOrEmpty(d.ChordAudioFilePath))?.ChordAudioFilePath;

                // Repoint every chart & lyric segment (including soft-deleted, to avoid dangling
                // FKs when the duplicate row is hard-deleted) onto the survivor.
                await _db.ChordCharts
                    .IgnoreQueryFilters()
                    .Where(cc => duplicateIds.Contains(cc.ChordId))
                    .ExecuteUpdateAsync(s => s.SetProperty(cc => cc.ChordId, canonical.Id), ct);

                await _db.LyricSegments
                    .IgnoreQueryFilters()
                    .Where(ls => duplicateIds.Contains(ls.ChordId))
                    .ExecuteUpdateAsync(s => s.SetProperty(ls => ls.ChordId, canonical.Id), ct);

                _db.Chords.RemoveRange(duplicates);
                mergedCount += duplicates.Count;
            }

            await _db.SaveChangesAsync(ct);
            return mergedCount;
        }

        private async Task<int> UpsertChordsAsync(IReadOnlyList<SeededVoicing> voicings, CancellationToken ct)
        {
            var distinctNames = voicings.Select(v => v.ChordName).Distinct().ToList();

            // Live rows only, grouped defensively: the Chords table has no unique index on
            // ChordName, so a straight ToDictionary by name can throw on stray duplicates.
            var existing = (await _db.Chords
                .IgnoreQueryFilters()
                .Where(c => (c.IsDeleted == false || c.IsDeleted == null) && distinctNames.Contains(c.ChordName))
                .ToListAsync(ct))
                .GroupBy(c => c.ChordName)
                .ToDictionary(g => g.Key, g => g.First());

            var inserted = 0;
            foreach (var name in distinctNames)
            {
                if (existing.ContainsKey(name)) continue;

                var chord = new Chord
                {
                    ChordName = name,
                    DateCreated = DateTimeOffset.UtcNow,
                    CreatedBy = SeedActor,
                    TenantId = null
                };
                _db.Chords.Add(chord);
                existing[name] = chord;
                inserted++;
            }

            if (inserted > 0) await _db.SaveChangesAsync(ct);
            return inserted;
        }

        private async Task<int> ReplaceStandardChartsAsync(IReadOnlyList<SeededVoicing> voicings, CancellationToken ct)
        {
            var existingStandard = await _db.ChordCharts
                .IgnoreQueryFilters()
                .Where(c => c.Source == ChordSource.Standard)
                .ToListAsync(ct);

            if (existingStandard.Count > 0)
            {
                _db.ChordCharts.RemoveRange(existingStandard);
                await _db.SaveChangesAsync(ct);
            }

            var voicingNames = voicings.Select(v => v.ChordName).Distinct().ToList();
            var chordIdByName = (await _db.Chords
                .IgnoreQueryFilters()
                .Where(c => (c.IsDeleted == false || c.IsDeleted == null) && voicingNames.Contains(c.ChordName))
                .ToListAsync(ct))
                .GroupBy(c => c.ChordName)
                .ToDictionary(g => g.Key, g => g.First().Id);

            var now = DateTimeOffset.UtcNow;
            foreach (var v in voicings)
            {
                if (!chordIdByName.TryGetValue(v.ChordName, out var chordId)) continue;

                _db.ChordCharts.Add(new ChordChart
                {
                    ChordId = chordId,
                    FretPosition = v.Position,
                    PositionDescription = v.DisplayLabel,
                    Source = ChordSource.Standard,
                    ChordDataJson = JsonSerializer.Serialize(v.Data, JsonOpts),
                    RenderedSvg = _renderer.Render(v.Data),
                    DateCreated = now,
                    CreatedBy = SeedActor,
                    TenantId = null
                });
            }

            await _db.SaveChangesAsync(ct);
            return voicings.Count;
        }

        private async Task UpsertSeedVersionAsync(SeedVersion? existing, string hash, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            if (existing is null)
            {
                _db.SeedVersions.Add(new SeedVersion
                {
                    SeedName = SeedKey,
                    Version = hash,
                    SeededAt = now,
                    DateCreated = DateTimeOffset.UtcNow,
                    CreatedBy = SeedActor,
                    TenantId = null
                });
            }
            else
            {
                existing.Version = hash;
                existing.SeededAt = now;
                existing.DateModified = DateTimeOffset.UtcNow;
                existing.ModifiedBy = SeedActor;
            }
            await _db.SaveChangesAsync(ct);
        }

        private string ResolveSourcePath()
        {
            var webRoot = (_env as IWebHostEnvironment)?.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            return Path.Combine(webRoot, SourceRelativePath[0], SourceRelativePath[1], SourceRelativePath[2]);
        }

        private static string ComputeFileHash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hashBytes = sha.ComputeHash(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
