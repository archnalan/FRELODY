using DocumentFormat.OpenXml.InkML;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYSHRD.Dtos.UserDtos;
using FRELODYSHRD.ModelTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SeedDataAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SongDbContext>();
                await CreateRolesAsync(scope,dbContext);
                var tenantId = await SeedDefaultTenantAsync(dbContext);
                if (!string.IsNullOrEmpty(tenantId))
                {
                    await SeedSDAHymnalSongBookAsync(tenantId, dbContext);
                    var sdaHymnal = await dbContext.SongBooks.FirstAsync(sb => sb.Slug == "sda-hymnal");
                    bool seeded = await CategoryData.Initialize(_serviceProvider, sdaHymnal.Id, tenantId);
                    if (seeded)
                    {
                        var mercyAndGraceCategory = await dbContext.Categories
                      .FirstAsync(c => c.Name.ToLower().Trim() == "grace and mercy of god" && c.SongBookId == sdaHymnal.Id);
                        await SeedAmazingGraceAsync(tenantId, dbContext);
                        await AttachAmazingGraceToSDAHymnalAsync(tenantId,dbContext, mercyAndGraceCategory.Id);

                    }

                    // More seeding methods here as needed
                    // await SeedCategoriesAsync(dbContext);
                    // await SeedChordsAsync(dbContext);
                    // etc.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private async Task CreateRolesAsync(IServiceScope scope, SongDbContext dbContext)
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            foreach (var roleName in UserRoles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Created role: {roleName}");
                    }
                    else
                    {
                        _logger.LogError($"Error creating role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private async Task<string?> SeedDefaultTenantAsync(SongDbContext dbContext)
        {
            const string defaultTenantName = "FRELODY";

            var existingTenant = await dbContext.Tenants
                .FirstOrDefaultAsync(t => t.TenantName == defaultTenantName);

            if (existingTenant != null)
            {
                _logger.LogInformation($"Default FRELODY tenant already exists with ID: {existingTenant.Id}");
                return existingTenant.Id;
            }
            _logger.LogInformation("Seeding default FRELODY tenant...");

            var defaultTenant = new Tenant
            {
                TenantName = defaultTenantName,
                BusinessRegNumber = "FREL-001",
                Address = "123 Music Avenue",
                City = "Harmony City",
                State = "Music State",
                PostalCode = "12345",
                Country = "United States",
                PhoneNumber = "+1-555-FRELODY",
                Email = "contact@frelody.com",
                Website = "https://www.frelody.com",
                Industry = "Music Technology",
                DateCreated= DateTime.UtcNow
            };

            await dbContext.Tenants.AddAsync(defaultTenant);
            await dbContext.SaveChangesAsync();
            await CreatePowerUserAsync(dbContext);
            _logger.LogInformation("Default FRELODY tenant seeded successfully.");
            return defaultTenant.Id;
        }

        private async Task CreatePowerUserAsync(SongDbContext dbContext)
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var powerUserEmail = _configuration["UserSettings:UserEmail"];
            var powerUserPassword = _configuration["UserSettings:UserPassword"];
            var powerUserName = _configuration["UserSettings:UserName"];
            if (string.IsNullOrWhiteSpace(powerUserEmail) || string.IsNullOrWhiteSpace(powerUserPassword))
            {
                _logger.LogWarning("Power user email or password is not configured.");
                return;
            }
            var existingUser = await userManager.FindByEmailAsync(powerUserEmail);
            if (existingUser != null)
            {              
                _logger.LogInformation("Power user already exists.");
                return;
            }
            var powerUser = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                UserName = powerUserName,
                Email = powerUserEmail,
                EmailConfirmed = true,
                UserType= UserType.SuperAdmin,
            };
            var user = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == powerUser.Email);
            if (user is null) user = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UserName == powerUser.UserName);
            if (user is not null)
            {
                _logger.LogInformation("Power user already exists in the database.");
                return;
            }
            var result = await userManager.CreateAsync(powerUser, powerUserPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Power user created successfully.");
                foreach (var roleName in UserRoles.AllRoles)
                {
                    if (await roleManager.RoleExistsAsync(roleName))
                    {
                        await userManager.AddToRoleAsync(powerUser, roleName);
                    }
                    else
                    {
                        _logger.LogWarning($"Role {roleName} does not exist. Cannot assign to power user.");
                    }
                }
            }
            else
            {
                _logger.LogError($"Error creating power user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        private async Task SeedAmazingGraceAsync(string defaultTenantId, SongDbContext dbContext)
        {
            // Check if song already exists
            if (await dbContext.Songs.AnyAsync(s => s.Title == "Amazing Grace"))
            {
                _logger.LogInformation("Amazing Grace song already exists in the database.");
                return;
            }

            _logger.LogInformation("Seeding Amazing Grace song...");

            // Ensure chords exist or create them
            var chordG = await GetOrCreateChordAsync(defaultTenantId, dbContext, "G");
            var chordG7 = await GetOrCreateChordAsync(defaultTenantId, dbContext, "G7");
            var chordC = await GetOrCreateChordAsync(defaultTenantId, dbContext, "C");
            var chordD = await GetOrCreateChordAsync(defaultTenantId,dbContext, "D");

            // Create the song
            var song = new Song
            {
                Title = "Amazing Grace",
                Slug = "amazing-grace",
                TenantId = defaultTenantId,
                Access = Access.Public
            };

            await dbContext.Songs.AddAsync(song);
            await dbContext.SaveChangesAsync();

            // Create the verse
            var verse = new SongPart
            {
                SongId = song.Id,
                PartNumber = 1,
                PartName = SongSection.Verse
            };

            await dbContext.SongParts.AddAsync(verse);
            await dbContext.SaveChangesAsync();

            // Create lyric lines
            var lines = new[]
            {
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 1 },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 2 },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 3 },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 4 },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 5 }
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

        private async Task<Chord> GetOrCreateChordAsync(string tenantId, SongDbContext dbContext, string chordName)
        {
            // Check if chord exists (case-insensitive, ignoring whitespace)
            var chord = await dbContext.Chords
                .FirstOrDefaultAsync(c => c.ChordName.Trim().ToLower() == chordName.Trim().ToLower());

            if (chord == null)
            {
                // Create new chord
                chord = new Chord
                {
                    ChordName = chordName,
                    TenantId= tenantId,
                    Access = Access.Public
                };
                await dbContext.Chords.AddAsync(chord);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"Created new chord: {chordName}");
            }

            return chord;
        }

        private async Task SeedSDAHymnalSongBookAsync(string defaultTenantId, SongDbContext dbContext)
        {
            const string sdaHymnalSlug = "sda-hymnal";
            if (await dbContext.SongBooks.AnyAsync(sb => sb.Slug == sdaHymnalSlug))
            {
                _logger.LogInformation("SDAHymnal songbook already exists in the database.");
                return;
            }

            var sdaHymnal = new SongBook
            {
                Title = "SDA Hymnal",
                Slug = sdaHymnalSlug,
                SubTitle = "Seventh-day Adventist Hymnal",
                Description = "Official hymnal of the Seventh-day Adventist Church.",
                Publisher = "Review and Herald Publishing Association",
                PublicationDate = new DateTime(1985, 1, 1),
                ISBN = "9780828010612",
                Author = "Various",
                Edition = "1985",
                Language = "English",
                TenantId = defaultTenantId,
                Access = Access.Public
            };

            await dbContext.SongBooks.AddAsync(sdaHymnal);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("SDAHymnal songbook seeded successfully.");
        }
        
        private async Task AttachAmazingGraceToSDAHymnalAsync(string defaultTenantId,SongDbContext dbContext, string categoryId)
        {
            // Get the SDA Hymnal songbook
            var sdaHymnal = await dbContext.SongBooks
                .FirstOrDefaultAsync(sb => sb.Slug == "sda-hymnal");
            if (sdaHymnal == null)
            {
                _logger.LogWarning("SDA Hymnal songbook not found. Cannot attach Amazing Grace.");
                return;
            }

            // Get the Amazing Grace song
            var amazingGrace = await dbContext.Songs
                .FirstOrDefaultAsync(s => s.Title == "Amazing Grace");
            if (amazingGrace == null)
            {
                _logger.LogWarning("Amazing Grace song not found. Cannot attach to SDA Hymnal.");
                return;
            }

            amazingGrace.CategoryId = categoryId; 
            amazingGrace.SongNumber = 108;

            dbContext.Songs.Update(amazingGrace);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Attached Amazing Grace as hymn 108 to SDA Hymnal.");
        }
    }
}
