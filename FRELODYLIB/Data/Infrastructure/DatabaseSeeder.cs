using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FRELODYAPP.Data.Infrastructure
{
    public class DatabaseSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SeedDataAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SongDbContext>();

                await SeedAmazingGraceAsync(dbContext);

                // More seeding methods here as needed
                // await SeedCategoriesAsync(dbContext);
                // await SeedChordsAsync(dbContext);
                // etc.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private async Task SeedAmazingGraceAsync(SongDbContext dbContext)
        {
            // Check if song already exists
            if (await dbContext.Songs.AnyAsync(s => s.Title == "Amazing Grace"))
            {
                _logger.LogInformation("Amazing Grace song already exists in the database.");
                return;
            }

            _logger.LogInformation("Seeding Amazing Grace song...");

            // Ensure chords exist or create them
            var chordG = await GetOrCreateChordAsync(dbContext, "G");
            var chordG7 = await GetOrCreateChordAsync(dbContext, "G7");
            var chordC = await GetOrCreateChordAsync(dbContext, "C");
            var chordD = await GetOrCreateChordAsync(dbContext, "D");

            // Create the song
            var song = new Song
            {
                Title = "Amazing Grace",
                Slug = "amazing-grace",
                SongNumber = 1
            };

            await dbContext.Songs.AddAsync(song);
            await dbContext.SaveChangesAsync();

            // Create the verse
            var verse = new Verse
            {
                SongId = song.Id,
                VerseNumber = 1
            };

            await dbContext.Verses.AddAsync(verse);
            await dbContext.SaveChangesAsync();

            // Create lyric lines
            var lines = new[]
            {
                new LyricLine { VerseId = verse.Id, PartName = SongSection.Verse, PartNumber = 1, LyricLineOrder = 1 },
                new LyricLine { VerseId = verse.Id, PartName = SongSection.Verse, PartNumber = 1, LyricLineOrder = 2 },
                new LyricLine { VerseId = verse.Id, PartName = SongSection.Verse, PartNumber = 1, LyricLineOrder = 3 },
                new LyricLine { VerseId = verse.Id, PartName = SongSection.Verse, PartNumber = 1, LyricLineOrder = 4 },
                new LyricLine { VerseId = verse.Id, PartName = SongSection.Verse, PartNumber = 1, LyricLineOrder = 5 }
            };

            await dbContext.LyricLines.AddRangeAsync(lines);
            await dbContext.SaveChangesAsync();

            // Create lyric segments
            var segments = new List<LyricSegment>
            {
                new LyricSegment { Lyric = "Amazing", LineNumber = 1, ChordId = chordG.Id, LyricLineId = lines[0].Id, LyricOrder = 1 },
                new LyricSegment { Lyric = "Grace", LineNumber = 1, ChordId = chordG7.Id, LyricLineId = lines[0].Id, LyricOrder = 2 },

                new LyricSegment { Lyric = "How", LineNumber = 2, ChordId = null, LyricLineId = lines[1].Id, LyricOrder = 1 },
                new LyricSegment { Lyric = "sweet the", LineNumber = 2, ChordId = chordC.Id, LyricLineId = lines[1].Id, LyricOrder = 2 },
                new LyricSegment { Lyric = "sound", LineNumber = 2, ChordId = chordG.Id, LyricLineId = lines[1].Id, LyricOrder = 3 },

                new LyricSegment { Lyric = "That saved a wretch like", LineNumber = 3, ChordId = null, LyricLineId = lines[2].Id, LyricOrder = 1 },
                new LyricSegment { Lyric = "me", LineNumber = 3, ChordId = chordD.Id, LyricLineId = lines[2].Id, LyricOrder = 2 },

                new LyricSegment { Lyric = "I", LineNumber = 4, ChordId = null, LyricLineId = lines[3].Id, LyricOrder = 1 },
                new LyricSegment { Lyric = "once was", LineNumber = 4, ChordId = chordG.Id, LyricLineId = lines[3].Id, LyricOrder = 2 },
                new LyricSegment { Lyric = "lost, but", LineNumber = 4, ChordId = chordG7.Id, LyricLineId = lines[3].Id, LyricOrder = 3 },
                new LyricSegment { Lyric = "now am", LineNumber = 4, ChordId = chordC.Id, LyricLineId = lines[3].Id, LyricOrder = 4 },
                new LyricSegment { Lyric = "found,", LineNumber = 4, ChordId = chordG.Id, LyricLineId = lines[3].Id, LyricOrder = 5 },

                new LyricSegment { Lyric = "Was", LineNumber = 5, ChordId = chordD.Id, LyricLineId = lines[4].Id, LyricOrder = 1 },
                new LyricSegment { Lyric = "blind, but", LineNumber = 5, ChordId = chordD.Id, LyricLineId = lines[4].Id, LyricOrder = 2 },
                new LyricSegment { Lyric = "now I", LineNumber = 5, ChordId = chordD.Id, LyricLineId = lines[4].Id, LyricOrder = 3 },
                new LyricSegment { Lyric = "see.", LineNumber = 5, ChordId = chordG.Id, LyricLineId = lines[4].Id, LyricOrder = 4 }
            };

            await dbContext.LyricSegments.AddRangeAsync(segments);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Amazing Grace song seeded successfully.");
        }

        private async Task<Chord> GetOrCreateChordAsync(SongDbContext dbContext, string chordName)
        {
            // Check if chord exists (case-insensitive, ignoring whitespace)
            var chord = await dbContext.Chords
                .FirstOrDefaultAsync(c => c.ChordName.Trim().ToLower() == chordName.Trim().ToLower());

            if (chord == null)
            {
                // Create new chord
                chord = new Chord
                {
                    ChordName = chordName
                };
                await dbContext.Chords.AddAsync(chord);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"Created new chord: {chordName}");
            }

            return chord;
        }
    }
}
