using DocumentFormat.OpenXml.InkML;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.Models;
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
                var (tenantId, userId) = await SeedDefaultTenantAsync(dbContext);
                if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(userId))
                {
                    await SeedSDAHymnalSongBookAsync(tenantId,userId, dbContext);
                    var sdaHymnal = await dbContext.SongBooks.FirstAsync(sb => sb.Slug == "sda-hymnal");
                    bool seeded = await CategoryData.Initialize(_serviceProvider, sdaHymnal.Id, tenantId,userId);
                    if (seeded)
                    {
                        var mercyAndGraceCategory = await dbContext.Categories
                      .FirstAsync(c => c.Name.ToLower().Trim() == "grace and mercy of god" && c.SongBookId == sdaHymnal.Id);
                        await SeedAmazingGraceAsync(tenantId,userId, dbContext);
                        await AttachAmazingGraceToSDAHymnalAsync(tenantId,userId, dbContext, mercyAndGraceCategory.Id);

                    }

                    await SeedProductsAsync(dbContext, tenantId, userId);
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

        private async Task<(string?,string?)> SeedDefaultTenantAsync(SongDbContext dbContext)
        {
            const string defaultTenantName = "FRELODY";

            var existingTenant = await dbContext.Tenants
                .FirstOrDefaultAsync(t => t.TenantName == defaultTenantName);

            if (existingTenant != null)
            {
                _logger.LogInformation($"Default FRELODY tenant already exists with ID: {existingTenant.Id}");
                return (existingTenant.Id, null);
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
            var userId =await CreatePowerUserAsync(dbContext);
            _logger.LogInformation("Default FRELODY tenant seeded successfully.");
            return (defaultTenant.Id, userId);
        }

        private async Task<string?> CreatePowerUserAsync(SongDbContext dbContext)
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
                return null;
            }
            // The platform superadmin must always carry the full role set (incl. SuperAdmin)
            // so platform-gated endpoints like /admin/products authorize correctly. We
            // reconcile on every startup — accounts seeded under an older role taxonomy
            // (e.g. only the deprecated Moderator) self-heal instead of staying stale.
            async Task EnsurePowerUserAsync(User u)
            {
                if (u.UserType != UserType.SuperAdmin)
                {
                    u.UserType = UserType.SuperAdmin;
                    await userManager.UpdateAsync(u);
                }
                var current = await userManager.GetRolesAsync(u);
                foreach (var roleName in UserRoles.AllRoles)
                {
                    if (current.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                        continue;
                    if (await roleManager.RoleExistsAsync(roleName))
                        await userManager.AddToRoleAsync(u, roleName);
                    else
                        _logger.LogWarning($"Role {roleName} does not exist. Cannot assign to power user.");
                }
            }

            var existingUser = await userManager.FindByEmailAsync(powerUserEmail);
            if (existingUser is null && !string.IsNullOrWhiteSpace(powerUserName))
                existingUser = await userManager.FindByNameAsync(powerUserName);
            if (existingUser is null)
            {
                // Fallback for tenant/soft-delete query filters hiding the row from UserManager.
                var dbUser = await dbContext.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Email == powerUserEmail || x.UserName == powerUserName);
                if (dbUser is not null)
                    existingUser = await userManager.FindByIdAsync(dbUser.Id);
            }
            if (existingUser is not null)
            {
                _logger.LogInformation("Power user already exists — reconciling roles.");
                await EnsurePowerUserAsync(existingUser);
                return existingUser.Id;
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
            var result = await userManager.CreateAsync(powerUser, powerUserPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Power user created successfully.");
                await EnsurePowerUserAsync(powerUser);
            }
            else
            {
                _logger.LogError($"Error creating power user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return powerUser.Id;
        }

        private async Task SeedAmazingGraceAsync(string defaultTenantId,string defaultUserId, SongDbContext dbContext)
        {
            // Check if song already exists
            if (await dbContext.Songs.AnyAsync(s => s.Title == "Amazing Grace"))
            {
                _logger.LogInformation("Amazing Grace song already exists in the database.");
                return;
            }

            _logger.LogInformation("Seeding Amazing Grace song...");

            // Ensure chords exist or create them
            var chordG = await GetOrCreateChordAsync(defaultTenantId, defaultUserId, dbContext, "G");
            var chordG7 = await GetOrCreateChordAsync(defaultTenantId, defaultUserId, dbContext, "G7");
            var chordC = await GetOrCreateChordAsync(defaultTenantId, defaultUserId, dbContext, "C");
            var chordD = await GetOrCreateChordAsync(defaultTenantId, defaultUserId, dbContext, "D");

            // Create the song
            var song = new Song
            {
                Title = "Amazing Grace",
                Slug = "amazing-grace",
                WrittenBy = "John Newton",
                CreatedBy = defaultUserId,
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
                PartName = SongSection.Verse,
                CreatedBy=defaultUserId,
                TenantId = defaultTenantId,
                Access = Access.Public
            };

            await dbContext.SongParts.AddAsync(verse);
            await dbContext.SaveChangesAsync();

            // Create lyric lines
            var lines = new[]
            {
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 1,TenantId= defaultTenantId, Access = Access.Public },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 2,TenantId= defaultTenantId, Access = Access.Public },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 3,TenantId= defaultTenantId, Access = Access.Public },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 4,TenantId= defaultTenantId, Access = Access.Public },
                new LyricLine { PartId = verse.Id, PartNumber = 1, LyricLineOrder = 5,TenantId= defaultTenantId, Access = Access.Public }
            };

            await dbContext.LyricLines.AddRangeAsync(lines);
            await dbContext.SaveChangesAsync();

            // Create lyric segments
            var segments = new List<LyricSegment>
            {
                new LyricSegment { Lyric = "Amazing", LineNumber = 1, ChordId = chordG.Id, LyricLineId = lines[0].Id, LyricOrder = 1,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "Grace", LineNumber = 1, ChordId = chordG7.Id, LyricLineId = lines[0].Id, LyricOrder = 2,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },

                new LyricSegment { Lyric = "How", LineNumber = 2, ChordId = null, LyricLineId = lines[1].Id, LyricOrder = 1,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "sweet the", LineNumber = 2, ChordId = chordC.Id, LyricLineId = lines[1].Id, LyricOrder = 2,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "sound", LineNumber = 2, ChordId = chordG.Id, LyricLineId = lines[1].Id, LyricOrder = 3,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },

                new LyricSegment { Lyric = "That saved a wretch like", LineNumber = 3, ChordId = null, LyricLineId = lines[2].Id, LyricOrder = 1,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "me", LineNumber = 3, ChordId = chordD.Id, LyricLineId = lines[2].Id, LyricOrder = 2,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },

                new LyricSegment { Lyric = "I", LineNumber = 4, ChordId = null, LyricLineId = lines[3].Id, LyricOrder = 1,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "once was", LineNumber = 4, ChordId = chordG.Id, LyricLineId = lines[3].Id, LyricOrder = 2,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "lost, but", LineNumber = 4, ChordId = chordG7.Id, LyricLineId = lines[3].Id, LyricOrder = 3,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "now am", LineNumber = 4, ChordId = chordC.Id, LyricLineId = lines[3].Id, LyricOrder = 4,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "found,", LineNumber = 4, ChordId = chordG.Id, LyricLineId = lines[3].Id, LyricOrder = 5,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },

                new LyricSegment { Lyric = "Was", LineNumber = 5, ChordId = chordD.Id, LyricLineId = lines[4].Id, LyricOrder = 1,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "blind, but", LineNumber = 5, ChordId = chordD.Id, LyricLineId = lines[4].Id, LyricOrder = 2,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "now I", LineNumber = 5, ChordId = chordD.Id, LyricLineId = lines[4].Id, LyricOrder = 3,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public },
                new LyricSegment { Lyric = "see.", LineNumber = 5, ChordId = chordG.Id, LyricLineId = lines[4].Id, LyricOrder = 4,TenantId=defaultTenantId,CreatedBy=defaultUserId,Access=Access.Public }
            };

            await dbContext.LyricSegments.AddRangeAsync(segments);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Amazing Grace song seeded successfully.");
        }

        private async Task<Chord> GetOrCreateChordAsync(string tenantId, string userId, SongDbContext dbContext, string chordName)
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
                    CreatedBy = userId,
                    Access = Access.Public
                };
                await dbContext.Chords.AddAsync(chord);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"Created new chord: {chordName}");
            }

            return chord;
        }

        private async Task SeedSDAHymnalSongBookAsync(string defaultTenantId,string defaultUserId, SongDbContext dbContext)
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
                CreatedBy = defaultUserId,
                TenantId = defaultTenantId,
                Access = Access.Public
            };

            await dbContext.SongBooks.AddAsync(sdaHymnal);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("SDAHymnal songbook seeded successfully.");
        }

        private async Task AttachAmazingGraceToSDAHymnalAsync(string defaultTenantId,string defaultUserId, SongDbContext dbContext, string categoryId)
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
            amazingGrace.ModifiedBy = defaultUserId;

            dbContext.Songs.Update(amazingGrace);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Attached Amazing Grace as hymn 108 to SDA Hymnal.");
        }

        private async Task SeedProductsAsync(SongDbContext dbContext, string tenantId, string userId)
        {
            _logger.LogInformation("Checking if products need seeding...");

            // Check if products already exist
            if (await dbContext.Products.AnyAsync())
            {
                _logger.LogInformation("Products already exist in the database.");
                return;
            }

            _logger.LogInformation("Seeding products...");

            // UGX prices — shown on /pricing (converted to local currency) and charged via PesaPal.
            decimal FREE_FOREVER = 0; // Free
            decimal CREATOR_MONTHLY = 30000; // Monthly subscription
            decimal CREATOR_ONE_TIME = 500000; // One-time payment (lifetime)
            decimal CREATOR_YEARLY = CREATOR_MONTHLY*12*0.8m; // Yearly subscription with 20% discount
            decimal STUDIO_MONTHLY = 100000; // Monthly subscription for studios

            // USD prices — charged via PayPal (which cannot settle UGX). The Product row is the
            // single source of truth for both currencies; PayPal reads PriceUsd directly.
            decimal FREE_FOREVER_USD = 0;
            decimal CREATOR_MONTHLY_USD = 10;
            decimal CREATOR_ONE_TIME_USD = 299;
            decimal CREATOR_YEARLY_USD = 99;
            decimal STUDIO_MONTHLY_USD = 29;

            var products = new List<Product>
    {
        new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Starter",
            Description = "2 song analyses every day — auto chords, slow-down & section loops. Free forever, no card.",
            Price = FREE_FOREVER,
            PriceUsd = FREE_FOREVER_USD,
            Currency = "UGX",
            Period = BillingPeriod.forever,
            IsPopular = false,
            Features = new List<Feature>
            {
                Feature.AutoChordDetection,
                Feature.SlowDownPractice,
                Feature.SectionLooping,
                Feature.ChordTimeline,
                Feature.PlaylistSaving,
                Feature.SongSharing
            },
            TenantId = tenantId,
            CreatedBy = userId,
            Access = Access.Public,
            DateCreated = DateTimeOffset.UtcNow
        },
        new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Creator Monthly",
            Description = "Unlimited song analyses and longer songs (up to 20 min). Billed monthly, cancel anytime.",
            Price = CREATOR_MONTHLY,
            PriceUsd = CREATOR_MONTHLY_USD,
            Currency = "UGX",
            Period = BillingPeriod.monthly,
            IsPopular = false,
            Features = new List<Feature>
            {
                Feature.AutoChordDetection,
                Feature.SlowDownPractice,
                Feature.SectionLooping,
                Feature.ChordTimeline,
                Feature.PlaylistSaving,
                Feature.SongSharing,
                Feature.PdfExport,
                Feature.UnlimitedAnalyses,
                Feature.ExtendedSongLength
            },
            TenantId = tenantId,
            CreatedBy = userId,
            Access = Access.Public,
            DateCreated = DateTimeOffset.UtcNow
        },
        new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Creator One-Time",
            Description = "Unlimited song analyses forever. One payment, lifetime access — no subscription.",
            Price = CREATOR_ONE_TIME,
            PriceUsd = CREATOR_ONE_TIME_USD,
            Currency = "UGX",
            Period = BillingPeriod.forever,
            IsPopular = true, // Most popular
            Features = new List<Feature>
            {
                Feature.AutoChordDetection,
                Feature.SlowDownPractice,
                Feature.SectionLooping,
                Feature.ChordTimeline,
                Feature.PlaylistSaving,
                Feature.SongSharing,
                Feature.PdfExport,
                Feature.UnlimitedAnalyses,
                Feature.ExtendedSongLength
            },
            TenantId = tenantId,
            CreatedBy = userId,
            Access = Access.Public,
            DateCreated = DateTimeOffset.UtcNow
        },
        new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Creator Yearly",
            Description = "Unlimited analyses and longer songs (up to 20 min). Billed yearly — save 20%.",
            Price = CREATOR_YEARLY,
            PriceUsd = CREATOR_YEARLY_USD,
            Currency = "UGX",
            Period = BillingPeriod.yearly,
            IsPopular = false,
            Features = new List<Feature>
            {
                Feature.AutoChordDetection,
                Feature.SlowDownPractice,
                Feature.SectionLooping,
                Feature.ChordTimeline,
                Feature.PlaylistSaving,
                Feature.SongSharing,
                Feature.PdfExport,
                Feature.UnlimitedAnalyses,
                Feature.ExtendedSongLength
            },
            TenantId = tenantId,
            CreatedBy = userId,
            Access = Access.Public,
            DateCreated = DateTimeOffset.UtcNow
        },
        new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Studio",
            Description = "For bands, churches & teams — shared library, member controls, bulk import and priority support.",
            Price = STUDIO_MONTHLY,
            PriceUsd = STUDIO_MONTHLY_USD,
            Currency = "UGX",
            Period = BillingPeriod.monthly,
            IsPopular = false,
            Features = new List<Feature>
            {
                Feature.AutoChordDetection,
                Feature.SlowDownPractice,
                Feature.SectionLooping,
                Feature.ChordTimeline,
                Feature.PlaylistSaving,
                Feature.SongSharing,
                Feature.PdfExport,
                Feature.UnlimitedAnalyses,
                Feature.ExtendedSongLength,
                Feature.PrioritySupport,
                Feature.SharedTeamLibrary
            },
            TenantId = tenantId,
            CreatedBy = userId,
            Access = Access.Public,
            DateCreated = DateTimeOffset.UtcNow
        }
    };

            await dbContext.Products.AddRangeAsync(products);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"Successfully seeded {products.Count} products.");
        }
    }
}
