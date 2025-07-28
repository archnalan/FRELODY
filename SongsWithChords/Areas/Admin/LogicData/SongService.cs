using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using FRELODYLIB.Interfaces;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class SongService : ISongService
    {
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
        public async Task<ServiceResult<List<ComboBoxDto>>> GetSongsAsync()
        {
            try
            {
                var songs = await _context.Songs
                    .Select(s => new ComboBoxDto
                    {
                        Id = s.SongNumber > 0 ? (int)s.SongNumber : 0,
                        ValueText = s.Title,
                        IdString = s.Id.ToString(),
                    })
                    .ToListAsync();
                return ServiceResult<List<ComboBoxDto>>.Success(songs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSongs");
                return ServiceResult<List<ComboBoxDto>>.Failure(ex);
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
                    // Create and save the Song first
                    var song = new Song
                    {
                        Title = songDto.Title,
                        SongNumber = songDto.SongNumber,
                        Slug = songDto.Title.ToLower().Replace(" ", "-")
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
                            var exisitingChord = await _context.Chords
                                .FirstOrDefaultAsync(c => c.ChordName.Trim().ToLower() == chordName.Trim().ToLower());

                            if(exisitingChord == null)
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
                                chordsWithIds[chordName] = exisitingChord.Id;
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

                        // Create the appropriate song part based on the type
                        string partId = await CreateSongPart(song.Id, partName, partNumber);

                        if (!string.IsNullOrEmpty(partId))
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
                                    LyricLineOrder = lineNumber
                                };

                                // Set the appropriate foreign key based on the part type
                                SetPartIdForLyricLine(lyricLine, partId, partName);

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
                                        LyricOrder = segmentDto.LyricOrder 
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

        private async Task<string> CreateSongPart(string songId, SongSection partType, int partNumber)
        {
            switch (partType)
            {
                case SongSection.Verse:
                    var verse = new SongPart
                    {
                        SongId = songId,
                        PartNumber = partNumber,
                        PartName = partType,
                    };
                    await _context.SongParts.AddAsync(verse);
                    await _context.SaveChangesAsync();
                    return verse.Id;

                default:
                    _logger.LogWarning("Unsupported song part type: {PartType}", partType);
                    return string.Empty;
            }
        }

        private void SetPartIdForLyricLine(LyricLine lyricLine, string partId, SongSection partType)
        {
            switch (partType)
            {
                case SongSection.Verse:
                    lyricLine.PartId = partId;
                    break;
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

        #region Get Song by Id
        public async Task<ServiceResult<SongDto>> GetSongById(string id)
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

        #region Get Song details by Id
        public async Task<ServiceResult<SongDto>> GetSongDetailsById(string id)
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
                        .Include(s => s.SongParts!)
                            .ThenInclude(v => v.LyricLines!)
                                .ThenInclude(ll => ll.LyricSegments!)
                        .FirstOrDefaultAsync(s => s.Id == id);

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

                        // Create the appropriate song part based on the type
                        string partId = await CreateSongPart(song.Id, partName, partNumber);

                        if (!string.IsNullOrEmpty(partId))
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
                                    LyricLineOrder = lineNumber
                                };

                                // Set the appropriate foreign key based on the part type
                                SetPartIdForLyricLine(lyricLine, partId, partName);

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
                                        LyricOrder = segmentDto.LyricOrder
                                    });
                                }

                                await _context.LyricSegments.AddRangeAsync(segments);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updatedSong = await GetSongById(song.Id);
                    if (updatedSong.IsSuccess)
                    {
                        return ServiceResult<SongDto>.Success(updatedSong.Data);
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
                var song = await _context.Songs
                    .FirstOrDefaultAsync(s => s.Id == songId);

                if (song == null) return ServiceResult<bool>.Failure(
                    new NotFoundException("Song not found."));

                song.IsFavorite = favorite;
                song.ModifiedBy = _userId;
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkSongAsFavorite");
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion
    }
}
