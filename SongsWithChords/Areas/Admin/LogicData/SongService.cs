using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Models;
using FRELODYLIB.Interfaces;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class SongService : ISongService
    {
        private const int MaxEditsPerRevision = 2;

        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly string _userId;
        private readonly ILogger<SongService> _logger;
        public SongService(SongDbContext context, ILogger<SongService> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
        }

        #region Get Songs
        public async Task<ServiceResult<PaginationDetails<ComboBoxDto>>> GetSongsAsync(int offset, int limit)
        {
            try
            {
                limit = limit <= 0 ? 10 : limit;

                var page = await _context.Songs
                        .OrderBy(s => s.SongNumber)
                        .ThenByDescending(s => s.Rating ?? 0)
                        .ToPaginatedResultAsync(offset, limit);

                var result = new PaginationDetails<ComboBoxDto>
                {
                    OffSet = page.OffSet,
                    Limit = page.Limit,
                    TotalSize = page.TotalSize,
                    HasMore = page.HasMore,
                    Data = page.Data?
                        .Select(s => new ComboBoxDto
                        {
                            ValueId = s.SongNumber.HasValue && s.SongNumber.Value > 0 ? s.SongNumber.Value : 0,
                            ValueText = s.Title,
                            IdString = s.Id
                        })
                        .ToList()
                };

                return ServiceResult<PaginationDetails<ComboBoxDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSongs");
                return ServiceResult<PaginationDetails<ComboBoxDto>>.Failure(ex);
            }
        }

        public async Task<ServiceResult<PaginationDetails<ComboBoxDto>>> SearchSongsAsync(string? keywords, int offset, int limit)
        {
            try
            {
                limit = limit <= 0 ? 10 : limit;

                // Filter on the entity
                var baseQuery = _context.Songs.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keywords))
                {
                    var kw = keywords.Trim();
                    if (int.TryParse(kw, out var songNumber))
                    {
                        baseQuery = baseQuery.Where(s =>
                            (s.SongNumber ?? 0) == songNumber ||
                            EF.Functions.Like(s.Title, $"%{kw}%") ||
                            (s.Slug != null && EF.Functions.Like(s.Slug, $"%{kw}%")));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(s =>
                            EF.Functions.Like(s.Title, $"%{kw}%") ||
                            (s.Slug != null && EF.Functions.Like(s.Slug, $"%{kw}%")));
                    }
                }

                var page = await baseQuery
                          .OrderBy(s => s.SongNumber)
                          .ThenByDescending(s => s.Rating ?? 0)
                          .ToPaginatedResultAsync(offset, limit);

                var result = new PaginationDetails<ComboBoxDto>
                {
                    OffSet = page.OffSet,
                    Limit = page.Limit,
                    TotalSize = page.TotalSize,
                    HasMore = page.HasMore,
                    Data = page.Data?
                        .Select(s => new ComboBoxDto
                        {
                            ValueId = s.SongNumber.HasValue && s.SongNumber.Value > 0 ? s.SongNumber.Value : 0,
                            ValueText = s.Title,
                            IdString = s.Id
                        })
                        .ToList()
                };

                return ServiceResult<PaginationDetails<ComboBoxDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchSongsAsync");
                return ServiceResult<PaginationDetails<ComboBoxDto>>.Failure(ex);
            }
        }
        #endregion

        #region Get Song by Category
        public async Task<ServiceResult<List<SongDto>>> GetSongsByCategory(string categoryId)
        {
            try
            {
                var songs = await _context.Songs
                    .Where(s => s.CategoryId == categoryId)
                    .OrderBy(s => s.SongNumber)
                    .ThenByDescending(s => s.Rating ?? 0)
                    .Select(s => new SongDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Slug = s.Slug,
                        SongNumber = s.SongNumber,
                        CategoryId = s.CategoryId,
                    })
                    .ToListAsync();
                return ServiceResult<List<SongDto>>.Success(songs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSongsByCategory");
                return ServiceResult<List<SongDto>>.Failure(ex);
            }
        }
        #endregion
               
        #region Create Song
        public async Task<ServiceResult<SongDto>> CreateSong(SimpleSongCreateDto songDto)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    bool categoryExists = false;
                    if( !string.IsNullOrEmpty(songDto.CategoryId))
                    {
                        categoryExists = await _context.Categories
                            .AnyAsync(c => c.Id == songDto.CategoryId);
                    }
                    var song = new Song
                    {
                        Title = songDto.Title,
                        SongNumber = songDto.SongNumber,
                        CategoryId = categoryExists ? songDto.CategoryId : null,
                        Slug = songDto.Title.ToLower().Replace(" ", "-"),
                        Rating = 0m
                    };
                    await _context.Songs.AddAsync(song);
                    await _context.SaveChangesAsync();

                    if (songDto.SongLyrics == null || !songDto.SongLyrics.Any())
                    {
                        await transaction.CommitAsync();
                        var songDtoResult = song.Adapt<SongDto>();
                        return ServiceResult<SongDto>.Success(songDtoResult);
                    }

                    //check and create chords if they are not already in the database
                    var chordsWithIds = new Dictionary<string, string>();

                    foreach(var segment in songDto.SongLyrics.Where(s=> !string.IsNullOrEmpty(s.ChordId)))
                    {
                        string chordName = segment.ChordName ?? string.Empty;
                        if (!string.IsNullOrEmpty(chordName)
                            && !chordsWithIds.ContainsKey(chordName))
                        {
                            var existingChord = await _context.Chords
                                .FirstOrDefaultAsync(c => c.ChordName.Trim().ToLower() == chordName.Trim().ToLower());

                            if(existingChord == null)
                            {
                                var newChord = new Chord
                                {
                                    ChordName = chordName.Trim()
                                };
                                await _context.Chords.AddAsync(newChord);
                                await _context.SaveChangesAsync();

                                chordsWithIds[chordName] = newChord.Id;
                            }
                            else
                            {
                                chordsWithIds[chordName] = existingChord.Id;
                            }
                        }
                        segment.ChordId = chordsWithIds[chordName];
                    }

                    // Group segments by song part and part number
                    var songPartGroups = songDto.SongLyrics
                        .GroupBy(s => new { s.PartName, s.PartNumber})
                        .OrderBy(g => g.Key.PartName)
                        .ThenBy(g => g.Key.PartNumber);

                    foreach (var partGroup in songPartGroups)
                    {
                        var partName = partGroup.Key.PartName;
                        var partNumber = partGroup.Key.PartNumber;
                        var partRepeatCount = songDto.PartRepeatCounts?.GetValueOrDefault(partName) ?? 0;

                        var part = new SongPart
                        {
                            SongId = song.Id,
                            PartNumber = partNumber,
                            PartName = partName,
                        };
                        await _context.SongParts.AddAsync(part);
                        await _context.SaveChangesAsync();

                        if (!string.IsNullOrEmpty(part.Id))
                        {
                            // Group segments by lyric line number
                            var lyricLineGroups = partGroup
                                .GroupBy(s => s.LineNumber)
                                .OrderBy(g => g.Key);

                            foreach (var lineGroup in lyricLineGroups)
                            {
                                var lineNumber = lineGroup.Key;
                                var lineRepeatCount = songDto.LineRepeatCounts?.GetValueOrDefault(lineNumber) ?? 0;

                                // Create the lyric line
                                var lyricLine = new LyricLine
                                {
                                    PartNumber = partNumber,
                                    LyricLineOrder = lineNumber,
                                    PartId = part.Id,
                                    RepeatCount = lineRepeatCount,
                                };

                                await _context.LyricLines.AddAsync(lyricLine);
                                await _context.SaveChangesAsync();

                                // Create lyric segments for this line
                                var segments = new List<LyricSegment>();

                                foreach (var segmentDto in lineGroup)
                                {
                                    segments.Add(new LyricSegment
                                    {
                                        Lyric = segmentDto.Lyric,
                                        LineNumber = lineNumber,
                                        LyricLineId = lyricLine.Id,
                                        ChordId = segmentDto.ChordId,
                                        LyricOrder = segmentDto.LyricOrder,
                                        ChordAlignment = segmentDto.ChordAlignment
                                    });
                                }

                                await _context.LyricSegments.AddRangeAsync(segments);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var createdSong = await GetSongById(song.Id);
                    if (createdSong.IsSuccess)
                    {
                        return ServiceResult<SongDto>.Success(createdSong.Data);
                    }
                    else
                    {
                        var songDtoResult = song.Adapt<SongDto>();
                        return ServiceResult<SongDto>.Success(songDtoResult);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in CreateSimpleSong: {Message}", ex.Message);
                    return ServiceResult<SongDto>.Failure(ex);
                }
            }
        }

        #endregion

        #region Create Lines
        private async Task<ServiceResult<bool>> LinesCreateAsync(ICollection<LineCreateDto> lines)
        {
            try
            {
                var lyricLines = lines.Adapt<List<LyricLine>>();
                await _context.LyricLines.AddRangeAsync(lyricLines);
                await _context.SaveChangesAsync();

                foreach (var line in lines)
                {
                    if (line.LyricSegments != null && line.LyricSegments.Any())
                    {
                        var lyricSegments = new List<LyricSegment>();

                        foreach (var segmentDto in line.LyricSegments)
                        {
                            var segment = segmentDto.Adapt<LyricSegment>();
                            segment.LineNumber = (int)line.LyricLineOrder;
                            segment.LyricLineId = lyricLines.FirstOrDefault(l => l.LyricLineOrder == segment.LineNumber)?.Id;
                            segment.LyricOrder = segmentDto.LyricOrder; // Use explicit segment number
                            lyricSegments.Add(segment);
                        }

                        await _context.LyricSegments.AddRangeAsync(lyricSegments);
                    }
                }
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Get full Song with chords by Id
        public async Task<ServiceResult<SongDto>> GetSongDetailsById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return ServiceResult<SongDto>.Failure(
                        new BadRequestException("Invalid song ID."));

                var song = await _context.Songs
                    .Include(s => s.SongParts.OrderBy(v => v.PartNumber))
                        .ThenInclude(v => v.LyricLines.OrderBy(ll => ll.LyricLineOrder))
                            .ThenInclude(ll => ll.LyricSegments.OrderBy(ls => ls.LyricOrder))
                                .ThenInclude(ls => ls.Chord)                    
                    .FirstOrDefaultAsync(s => s.Id == id);

                var songDto = song.Adapt<SongDto>();
                return ServiceResult<SongDto>.Success(songDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSongById");
                return ServiceResult<SongDto>.Failure(ex);
            }
        }
        #endregion

        #region Get Song by Id
        public async Task<ServiceResult<SongDto>> GetSongById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return ServiceResult<SongDto>.Failure(
                        new BadRequestException("Invalid song ID."));

                var song = await _context.Songs.FirstOrDefaultAsync(s => s.Id == id);

                var songDto = song.Adapt<SongDto>();
                return ServiceResult<SongDto>.Success(songDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSongById");
                return ServiceResult<SongDto>.Failure(ex);
            }
        }
        #endregion

        #region Update Song
        public async Task<ServiceResult<SongDto>> UpdateSong(string id, SimpleSongCreateDto songDto)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        return ServiceResult<SongDto>.Failure(
                            new BadRequestException("Song ID is required."));
                    }
                    // Find the song to update
                    var song = await _context.Songs
                        .Where(s => s.Id == id)
                            .Include(s => s.SongParts!)
                                .ThenInclude(v => v.LyricLines!)
                                    .ThenInclude(ll => ll.LyricSegments!)
                        .FirstOrDefaultAsync();

                    if (song == null)
                    {
                        return ServiceResult<SongDto>.Failure(
                            new BadRequestException("Song not found."));
                    }

                    // Update basic song properties
                    song.Title = songDto.Title;
                    song.SongNumber = songDto.SongNumber;
                    song.Slug = songDto.Title.ToLower().Replace(" ", "-");

                    // If no lyrics to update, just save the song changes
                    if (songDto.SongLyrics == null || !songDto.SongLyrics.Any())
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        var songDtoResult = song.Adapt<SongDto>();
                        return ServiceResult<SongDto>.Success(songDtoResult);
                    }

                    // Clear existing song parts and their lyrics
                    if (song.SongParts != null && song.SongParts.Any())
                    {
                        foreach (var verse in song.SongParts)
                        {
                            if (verse.LyricLines != null && verse.LyricLines.Any())
                            {
                                foreach (var line in verse.LyricLines)
                                {
                                    if (line.LyricSegments != null)
                                    {
                                        _context.LyricSegments.RemoveRange(line.LyricSegments);
                                    }
                                    _context.LyricLines.Remove(line);
                                }
                            }
                            _context.SongParts.Remove(verse);
                        }
                    }

                    await _context.SaveChangesAsync();                    
                    
                    // Process chords - identify and create new ones
                    var chordsWithIds = new Dictionary<string, string>();

                    foreach (var segment in songDto.SongLyrics.Where(s => !string.IsNullOrEmpty(s.ChordName)))
                    {
                        string chordName = segment.ChordName?.Trim() ?? string.Empty;
                        if (!string.IsNullOrEmpty(chordName) && !chordsWithIds.ContainsKey(chordName))
                        {
                            var existingChord = await _context.Chords
                                .FirstOrDefaultAsync(c => c.ChordName.Trim().ToLower() == chordName.ToLower());

                            if (existingChord == null)
                            {
                                var newChord = new Chord
                                {
                                    ChordName = chordName
                                };
                                await _context.Chords.AddAsync(newChord);
                                await _context.SaveChangesAsync();

                                chordsWithIds[chordName] = newChord.Id;
                            }
                            else
                            {
                                chordsWithIds[chordName] = existingChord.Id;
                            }
                        }
                        segment.ChordId = chordsWithIds[chordName];
                    }

                    // Group segments by song part and part number
                    var songPartGroups = songDto.SongLyrics
                        .GroupBy(s => new { s.PartName, s.PartNumber })
                        .OrderBy(g => g.Key.PartName)
                        .ThenBy(g => g.Key.PartNumber);

                    foreach (var partGroup in songPartGroups)
                    {
                        var partName = partGroup.Key.PartName;
                        var partNumber = partGroup.Key.PartNumber;

                        var part = new SongPart
                        {
                            SongId = song.Id,
                            PartNumber = partNumber,
                            PartName = partName,
                        };
                        await _context.SongParts.AddAsync(part);
                        await _context.SaveChangesAsync();

                        if (!string.IsNullOrEmpty(part.Id))
                        {
                            // Group segments by lyric line number
                            var lyricLineGroups = partGroup
                                .GroupBy(s => s.LineNumber)
                                .OrderBy(g => g.Key);

                            foreach (var lineGroup in lyricLineGroups)
                            {
                                var lineNumber = lineGroup.Key;

                                // Create the lyric line
                                var lyricLine = new LyricLine
                                {
                                    PartNumber = partNumber,
                                    LyricLineOrder = lineNumber,
                                    PartId = part.Id,
                                };

                                await _context.LyricLines.AddAsync(lyricLine);
                                await _context.SaveChangesAsync();

                                // Create lyric segments for this line
                                var segments = new List<LyricSegment>();

                                foreach (var segmentDto in lineGroup)
                                {
                                    segments.Add(new LyricSegment
                                    {
                                        Lyric = segmentDto.Lyric,
                                        LineNumber = lineNumber,
                                        LyricLineId = lyricLine.Id,
                                        ChordId = segmentDto.ChordId,
                                        LyricOrder = segmentDto.LyricOrder,
                                        ChordAlignment = segmentDto.ChordAlignment
                                    });
                                }

                                await _context.LyricSegments.AddRangeAsync(segments);
                            }
                        }
                    }
                    song.ModifiedBy = _userId;
                    song.Revision++;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updatedSong = await GetSongById(song.Id);
                    return updatedSong.IsSuccess
                        ? ServiceResult<SongDto>.Success(updatedSong.Data)
                        : ServiceResult<SongDto>.Success(song.Adapt<SongDto>());
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in UpdateSong: {Message}", ex.Message);
                    return ServiceResult<SongDto>.Failure(ex);
                }
            }
        }
        #endregion

        #region Mark song as favorite
        public async Task<ServiceResult<bool>> MarkSongFavoriteStatus(string songId, bool favorite)
        {
            try
            {
                if (string.IsNullOrEmpty(songId))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Song ID is required."));
                }
                var song = await _context.Songs.FirstOrDefaultAsync(s => s.Id == songId);
                if (song == null)
                    return ServiceResult<bool>.Failure(new NotFoundException("Song not found."));

                var existing = await _context.SongUserFavorites
                    .FirstOrDefaultAsync(f => f.SongId == songId && f.UserId == _userId);

                if (existing != null)
                {
                    // Remove favorite
                    _context.SongUserFavorites.Remove(existing);
                }
                else
                {
                    // Add favorite
                    _context.SongUserFavorites.Add(new SongUserFavorite
                    {
                        SongId = songId,
                        UserId = _userId,
                        FavoritedAt = DateTimeOffset.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkSongAsFavorite {SongId}", songId);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Set Song Rating
        public async Task<ServiceResult<CanRateDto>> CanUserRateSong(string songId)
        {
            try
            {
                if (string.IsNullOrEmpty(songId))
                    return ServiceResult<CanRateDto>.Failure(new BadRequestException("Song ID is required."));

                var song = await _context.Songs.FirstOrDefaultAsync(s => s.Id == songId);
                if (song is null)
                    return ServiceResult<CanRateDto>.Failure(new NotFoundException("Song not found."));

                var userId = _userId;
                var existing = await _context.SongUserRatings
                    .FirstOrDefaultAsync(r => r.SongId == songId && r.UserId == userId);

                var aggQ = _context.SongUserRatings.Where(r => r.SongId == songId);
                var total = await aggQ.CountAsync();
                var avg = total > 0 ? await aggQ.AverageAsync(r => r.Rating) : 0m;

                var dto = new CanRateDto
                {
                    AggregateRating = total > 0 ? Math.Round(avg, 2, MidpointRounding.AwayFromZero) : null,
                    TotalRatings = total,
                    MaxEdits = MaxEditsPerRevision
                };

                if (existing is null)
                {
                    dto.CanRate = true;
                    dto.EditsRemaining = MaxEditsPerRevision;
                    return ServiceResult<CanRateDto>.Success(dto);
                }

                // If song was revised after their last rating: reset allowance for the new revision
                if (existing.RevisionAtRating < song.Revision)
                {
                    dto.CanRate = true;
                    dto.YourRating = existing.Rating;
                    dto.EditsRemaining = MaxEditsPerRevision;
                    dto.Reason = "Song has changed. You can rate this version.";
                    return ServiceResult<CanRateDto>.Success(dto);
                }

                // Same revision: compute remaining edits
                var remaining = Math.Max(0, MaxEditsPerRevision - existing.ModificationCount);
                dto.YourRating = existing.Rating;
                dto.EditsRemaining = remaining;

                if (remaining > 0)
                {
                    dto.CanRate = true;
                    dto.Reason = $"You can adjust your rating. {remaining} edit(s) left.";
                }
                else
                {
                    dto.CanRate = false;
                    dto.Reason = "You’ve reached the maximum number of rating edits for this version.";
                }

                return ServiceResult<CanRateDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CanUserRateSong");
                return ServiceResult<CanRateDto>.Failure(ex);
            }
        }

        public async Task<ServiceResult<bool>> SetSongRating(string songId, decimal rating)
        {
            try
            {
                if (string.IsNullOrEmpty(songId))
                    return ServiceResult<bool>.Failure(new BadRequestException("Song ID is required."));
                if (rating < 0 || rating > 5)
                    return ServiceResult<bool>.Failure(new BadRequestException("Rating must be between 0 and 5."));

                var song = await _context.Songs.FirstOrDefaultAsync(s => s.Id == songId);
                if (song is null)
                    return ServiceResult<bool>.Failure(new NotFoundException("Song not found."));

                var userId = string.IsNullOrEmpty(_userId) ? null : _userId;
                var existing = await _context.SongUserRatings
                    .FirstOrDefaultAsync(r => r.SongId == songId && r.UserId == userId);

                var rounded = Math.Round(rating, 2, MidpointRounding.AwayFromZero);

                if (existing is null)
                {
                    _context.SongUserRatings.Add(new SongUserRating
                    {
                        SongId = songId,
                        UserId = userId,
                        Rating = rounded,
                        RevisionAtRating = song.Revision,
                        ModificationCount = 0,
                        RatedAt = DateTimeOffset.UtcNow
                    });
                }
                else if (existing.RevisionAtRating < song.Revision)
                {
                    // New revision: reset edit counter
                    existing.Rating = rounded;
                    existing.RevisionAtRating = song.Revision;
                    existing.ModificationCount = 0;
                    existing.RatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    // Same revision: enforce edit cap
                    if (existing.ModificationCount >= MaxEditsPerRevision)
                        return ServiceResult<bool>.Failure(new BadRequestException("You've reached the maximum number of rating edits for this version."));

                    existing.Rating = rounded;
                    existing.ModificationCount += 1;
                    existing.RatedAt = DateTimeOffset.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Refresh aggregate rating on Song
                var q = _context.SongUserRatings.Where(r => r.SongId == songId);
                var count = await q.CountAsync();
                var avg = count > 0 ? await q.AverageAsync(r => r.Rating) : 0m;

                song.Rating = Math.Round(avg, 2, MidpointRounding.AwayFromZero);
                song.ModifiedBy = userId;
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetSongRating");
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Get Favorite Songs
        public async Task<ServiceResult<PaginationDetails<ComboBoxDto>>> GetFavoriteSongs(string? userId = null, int? offset= 0, int? limit = 10)
        {
            try
            {
                userId ??= _userId;
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<PaginationDetails<ComboBoxDto>>.Failure(new BadRequestException("User must be authenticated."));
                
                offset = Math.Max(0, offset ?? 0);
                limit = Math.Min(limit ?? 0, 100);
                var songs = await _context.SongUserFavorites
                    .Where(f => f.UserId == _userId)
                    .Include(f => f.Song)
                    .OrderByDescending(f => f.FavoritedAt)
                    .Select(s => new ComboBoxDto
                    {
                        Id = s.Id,
                        IdString = s.Id,
                        ValueId = s.Song!.SongNumber.HasValue && s.Song.SongNumber.Value > 0 ? s.Song.SongNumber.Value : 0,
                        ValueText = s.Song.Title,
                    })
                    .ToPaginatedResultAsync(offset.Value, limit.Value);

                return ServiceResult<PaginationDetails<ComboBoxDto>>.Success(songs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFavoriteSongs");
                return ServiceResult<PaginationDetails<ComboBoxDto>>.Failure(ex);
            }
        }

        public async Task<ServiceResult<bool>> IsSongFavorited(string songId, string? userId = null)
        {
            try
            {
                userId ??= _userId;
                if (string.IsNullOrEmpty(songId) || string.IsNullOrEmpty(_userId))
                    return ServiceResult<bool>.Success(false);

                var isFavorited = await _context.SongUserFavorites
                    .AnyAsync(f => f.SongId == songId && f.UserId == _userId);

                return ServiceResult<bool>.Success(isFavorited);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if song {SongId} is favorited", songId);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Soft Delete Song 
        public async Task<ServiceResult<bool>> DeleteSong(string songId)
        {
            try
            {
                if (string.IsNullOrEmpty(songId))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Song ID is required."));
                }
                var song = await _context.Songs
                    .FirstOrDefaultAsync(s => s.Id == songId);
                if (song == null) return ServiceResult<bool>.Failure(
                    new NotFoundException("Song not found."));
                song.IsDeleted = true;
                song.ModifiedBy = _userId;
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSong");
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion
    }
}
