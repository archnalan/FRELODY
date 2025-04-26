using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
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
        private readonly ILogger<SongService> _logger;
        public SongService(SongDbContext context, ILogger<SongService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Create Song
        public async Task<ServiceResult<SongDto>> CreateFullSong(FullSongCreateDto s)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create and save the Song first
                    var song = s.Adapt<Song>();
                    song.Slug = s.Title.ToLower().Replace(" ", "-");
                    await _context.Songs.AddAsync(song);
                    await _context.SaveChangesAsync();

                    // Process verses and their nested entities
                    if (s.Verses != null && s.Verses.Any())
                    {
                        var verses = await CreateSongParts<Verse, VerseCreateDto>(
                            s.Verses,
                            song.Id,
                            (verse, songId) => verse.SongId = songId);

                        await ProcessLyricLinesForParts(s.Verses, verses,
                            (line, partId) => line.VerseId = partId,
                            (lineDto, part) => part.VerseNumber == lineDto.VerseNumber,
                            SongSection.Verse);
                    }

                    if (s.Bridges != null && s.Bridges.Any())
                    {
                        var bridges = await CreateSongParts<Bridge, BridgeCreateDto>(
                            s.Bridges,
                            song.Id,
                            (bridge, songId) => bridge.SongId = songId);

                        await ProcessLyricLinesForParts(s.Bridges, bridges,
                            (line, partId) => line.BridgeId = partId,
                            (lineDto, part) => part.BridgeNumber == lineDto.BridgeNumber,
                            SongSection.Bridge);
                    }

                    if (s.Choruses != null && s.Choruses.Any())
                    {
                        var choruses = await CreateSongParts<Chorus, ChorusCreateDto>(
                            s.Choruses,
                            song.Id,
                            (chorus, songId) => chorus.SongId = songId);

                        await ProcessLyricLinesForParts(s.Choruses, choruses,
                            (line, partId) => line.ChorusId = partId,
                            (lineDto, part) => part.ChorusNumber == lineDto.ChorusNumber,
                            SongSection.Chorus);
                    }

                    await transaction.CommitAsync();
                    var createdSong = await GetSongById(song.Id);
                    if (createdSong.IsSuccess)
                    {
                        return ServiceResult<SongDto>.Success(createdSong.Data);
                    }
                    else
                    {
                        var songDto = song.Adapt<SongDto>();
                        return ServiceResult<SongDto>.Success(songDto);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in CreateFullSong");
                    return ServiceResult<SongDto>.Failure(ex);
                }
            }
        }

        private async Task<List<TPart>> CreateSongParts<TPart, TDto>(
            IEnumerable<TDto> partDtos,
            Guid songId,
            Action<TPart, Guid> setSongId)
            where TPart : class
        {
            var parts = new List<TPart>();
            foreach (var dto in partDtos)
            {
                var part = dto.Adapt<TPart>();
                setSongId(part, songId);
                parts.Add(part);
            }

            await _context.Set<TPart>().AddRangeAsync(parts);
            await _context.SaveChangesAsync();
            return parts;
        }

        private async Task ProcessLyricLinesForParts<TPart, TDto>(
            IEnumerable<TDto> partDtos,
            List<TPart> savedParts,
            Action<LyricLine, Guid> setPartId,
            Func<TDto, TPart, bool> matchPartFunc,
            SongSection sectionType)
            where TDto : ISongPartDto
        {
            foreach (var partDto in partDtos)
            {
                if (partDto.LyricLines != null && partDto.LyricLines.Count > 0)
                {
                    var savedPart = savedParts.FirstOrDefault(p => matchPartFunc(partDto, p));
                    if (savedPart != null)
                    {
                        var partId = (Guid)savedPart.GetType().GetProperty("Id").GetValue(savedPart);
                        var partNumber = (int)partDto.GetPartNumber();

                        var lyricLines = new List<LyricLine>();
                        foreach (var lineDto in partDto.LyricLines)
                        {
                            var line = lineDto.Adapt<LyricLine>();
                            line.PartNumber = partNumber;
                            line.PartName = sectionType;
                            setPartId(line, partId);
                            lyricLines.Add(line);
                        }

                        await _context.LyricLines.AddRangeAsync(lyricLines);
                        await _context.SaveChangesAsync();

                        foreach (var lineDto in partDto.LyricLines)
                        {
                            if (lineDto.LyricSegments != null && lineDto.LyricSegments.Any())
                            {
                                var line = lyricLines.FirstOrDefault(l => l.LyricLineOrder == lineDto.LyricLineOrder);
                                if (line != null)
                                {
                                    var segments = new List<LyricSegment>();
                                    foreach (var segmentDto in lineDto.LyricSegments)
                                    {
                                        var segment = segmentDto.Adapt<LyricSegment>();
                                        segment.LineNumber = (int)lineDto.LyricLineOrder;
                                        segment.LyricLineId = line.Id;
                                        // Use the segment's order value if available, otherwise use position
                                        segment.LyricOrder = segmentDto.LyricOrder;
                                        segments.Add(segment);
                                    }
                                    await _context.LyricSegments.AddRangeAsync(segments);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
        #endregion

        #region Create Simple Song
        public async Task<ServiceResult<SongDto>> CreateSimpleSong(SimpleSongCreateDto songDto)
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
                        Guid partId = await CreateSongPart(song.Id, partName, partNumber);

                        if (partId != Guid.Empty)
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
                                    PartName = partName,
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
                                        LyricOrder = segmentDto.SegmentOrder // Use the explicit segment order
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

        private async Task<Guid> CreateSongPart(Guid songId, SongSection partType, int partNumber)
        {
            switch (partType)
            {
                case SongSection.Verse:
                    var verse = new Verse
                    {
                        SongId = songId,
                        VerseNumber = partNumber
                    };
                    await _context.Verses.AddAsync(verse);
                    await _context.SaveChangesAsync();
                    return verse.Id;

                case SongSection.Chorus:
                    var chorus = new Chorus
                    {
                        SongId = songId,
                        ChorusNumber = partNumber
                    };
                    await _context.Choruses.AddAsync(chorus);
                    await _context.SaveChangesAsync();
                    return chorus.Id;

                case SongSection.Bridge:
                    var bridge = new Bridge
                    {
                        SongId = songId,
                        BridgeNumber = partNumber
                    };
                    await _context.Bridges.AddAsync(bridge);
                    await _context.SaveChangesAsync();
                    return bridge.Id;

                default:
                    _logger.LogWarning("Unsupported song part type: {PartType}", partType);
                    return Guid.Empty;
            }
        }

        private void SetPartIdForLyricLine(LyricLine lyricLine, Guid partId, SongSection partType)
        {
            switch (partType)
            {
                case SongSection.Verse:
                    lyricLine.VerseId = partId;
                    break;
                case SongSection.Chorus:
                    lyricLine.ChorusId = partId;
                    break;
                case SongSection.Bridge:
                    lyricLine.BridgeId = partId;
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
        public async Task<ServiceResult<SongDto>> GetSongById(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<SongDto>.Failure(
                        new BadRequestException("Invalid song ID."));

                var song = await _context.Songs
                    .Include(s => s.Verses.OrderBy(v => v.VerseNumber))
                        .ThenInclude(v => v.LyricLines.OrderBy(ll => ll.LyricLineOrder))
                            .ThenInclude(ll => ll.LyricSegments.OrderBy(ls => ls.LyricOrder))
                                .ThenInclude(ls => ls.Chord)
                    .Include(s => s.Bridges.OrderBy(b => b.BridgeNumber))
                        .ThenInclude(b => b.LyricLines.OrderBy(ll => ll.LyricLineOrder))
                            .ThenInclude(ll => ll.LyricSegments.OrderBy(ls => ls.LyricOrder))
                                .ThenInclude(ls => ls.Chord)
                    .Include(s => s.Choruses.OrderBy(c => c.ChorusNumber))
                        .ThenInclude(c => c.LyricLines.OrderBy(ll => ll.LyricLineOrder))
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
    }
}
