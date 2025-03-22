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

                    // Process bridges and their nested entities
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

                    // Process choruses and their nested entities
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

        /// <summary>
        /// Creates song parts (verses, bridges, choruses) with proper foreign key relationships
        /// </summary>
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

        /// <summary>
        /// Processes lyric lines and segments for song parts (verses, bridges, choruses)
        /// </summary>
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
                    // Find the saved part entity
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

                        // Now add lyric segments for each line
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

        
        #region Get Song by Id
        public async Task<ServiceResult<SongDto>> GetSongById(Guid id)
        {
            try
            {
                var song = await _context.Songs
                    .Include(s => s.Verses)
                        .ThenInclude(v => v.LyricLines)
                            .ThenInclude(ll => ll.LyricSegments)
                    .Include(s => s.Bridges)
                        .ThenInclude(b => b.LyricLines)
                            .ThenInclude(ll => ll.LyricSegments)
                    .Include(s => s.Choruses)
                        .ThenInclude(c => c.LyricLines)
                            .ThenInclude(ll => ll.LyricSegments)
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
