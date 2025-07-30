using Mapster;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web.Http.ModelBinding;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using FRELODYSHRD.Dtos;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class ChordService : IChordService
	{
		private readonly SongDbContext _context;
        private readonly ILogger<ChordService> _logger;
        public ChordService(SongDbContext context, ILogger<ChordService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<List<ChordSimpleDto>>> GetChordsAsync()
		{
			try
            {
                var chords = await _context.Chords
                                .OrderBy(c => c.ChordName)
                                .ToListAsync();

                var chordsDto = chords.Adapt<List<ChordSimpleDto>>();

                return ServiceResult<List<ChordSimpleDto>>.Success(chordsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chords:{Error}", ex);
                return ServiceResult<List<ChordSimpleDto>>.Failure(new
                    Exception($"Error retrieving chords. Details: {ex.Message}"));
            }
           
		}
		
		public async Task<ServiceResult<List<ChordWithChartsDto>>> GetChordsWithChartsAsync()
		{
			try
			{
                var chords = await _context.Chords
                               .OrderBy(c => c.ChordName)
                               .Include(ch => ch.ChordCharts)
                               .ToListAsync();

                var chordsDto = chords.Adapt<List<ChordWithChartsDto>>();

                return ServiceResult<List<ChordWithChartsDto>>.Success(chordsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chords with charts: {Error}", ex);
                return ServiceResult<List<ChordWithChartsDto>>.Failure(new
                    Exception($"Error retrieving chords with charts. Details: {ex.Message}"));
            }   
		}

		public async Task<ServiceResult<ChordEditDto>> GetChordByIdAsync(string id)
		{
			try
			{
                var chord = await _context.Chords.FindAsync(id);

                if (chord == null) return ServiceResult<ChordEditDto>.Failure(new
                    NotFoundException($"Chord with ID: {id} does not exist."));

                var chordDto = chord.Adapt<Chord, ChordEditDto>();

                return ServiceResult<ChordEditDto>.Success(chordDto);

            }
			catch(Exception ex)
			{
                _logger.LogError(ex, "Error retrieving chord by ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<ChordEditDto>.Failure(new
                    Exception($"Error retrieving chord with ID: {id}. Details: {ex.Message}"));
            }
			
		}

        public async Task<ServiceResult<ChordWithChartsDto>> GetChordWithChartsByIdAsync(string id)
        {
            try
            {
                var chord = await _context.Chords
                        .Include(ch => ch.ChordCharts)
                        .FirstOrDefaultAsync(ch => ch.Id == id);

                if (chord == null) return ServiceResult<ChordWithChartsDto>.Failure(new
                    NotFoundException($"Chord with ID: {id} does not exist."));

                if (chord.ChordCharts != null)
                {
                    chord.ChordCharts = chord.ChordCharts.OrderBy(cc => cc.FretPosition).ToList();
                }

                var chordDto = chord.Adapt<Chord, ChordWithChartsDto>();

                return ServiceResult<ChordWithChartsDto>.Success(chordDto);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chord with charts by ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<ChordWithChartsDto>.Failure(new
                    Exception($"Error retrieving chord with charts by ID: {id}. Details: {ex.Message}"));
            }

        }

		public async Task<ServiceResult<ChordEditDto>> CreateChordAsync(ChordCreateDto chordDto)
		{
			try
			{
                if (chordDto == null) return ServiceResult<ChordEditDto>.Failure(new
                BadRequestException("Chord data is Required"));

                var chordExists = await _context.Chords
                                .AnyAsync(ch => ch.ChordName == chordDto.ChordName);

                if (chordExists) return ServiceResult<ChordEditDto>.Failure(new
                    ConflictException($"Chord: {chordDto.ChordName} already exists."));

                var chord = chordDto.Adapt<ChordCreateDto, Chord>();

                try
                {
                    await _context.Chords.AddAsync(chord);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return ServiceResult<ChordEditDto>.Failure(new Exception(ex.Message));
                }

                var newChord = chord.Adapt<Chord, ChordEditDto>();

                return ServiceResult<ChordEditDto>.Success(newChord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chord: {ChordName}. Error: {Error}", chordDto.ChordName, ex);
                return ServiceResult<ChordEditDto>.Failure(new
                    Exception($"Error creating chord. Details: {ex.Message}"));
            }
            
		}

        public async Task<ServiceResult<ChordSimpleDto>> CreateSimpleChordAsync(ChordSimpleDto chordDto)
        {
            try
            {
                if (chordDto == null) return ServiceResult<ChordSimpleDto>.Failure(new
                BadRequestException("Chord data is Required"));

                var chordExists = await _context.Chords
                                .AnyAsync(ch => ch.ChordName == chordDto.ChordName);

                if (chordExists) return ServiceResult<ChordSimpleDto>.Failure(new
                    ConflictException($"Chord: {chordDto.ChordName} already exists."));

                var chord = chordDto.Adapt<Chord>();


                await _context.Chords.AddAsync(chord);
                await _context.SaveChangesAsync();


                var newChord = chord.Adapt<ChordSimpleDto>();

                return ServiceResult<ChordSimpleDto>.Success(newChord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simple chord: {ChordName}. Error: {Error}", chordDto.ChordName, ex);
                return ServiceResult<ChordSimpleDto>.Failure(new
                    Exception($"Error creating simple chord. Details: {ex.Message}"));
            }

        }

        public async Task<ServiceResult<ChordEditDto>> UpdateChordAsync(ChordEditDto chordDto)
		{
           
			if (chordDto == null) return ServiceResult<ChordEditDto>.Failure(new
				BadRequestException("Chord data is Required"));

			var chord = await _context.Chords.FindAsync(chordDto.Id);
			if (chord == null) return ServiceResult<ChordEditDto>.Failure(new
				NotFoundException($"Chord with ID: {chordDto.Id} does not exist."));

			var chordExists = await _context.Chords
							.AnyAsync(ch => ch.ChordName == chordDto.ChordName && ch.Id != chordDto.Id);
			if (chordExists) return ServiceResult<ChordEditDto>.Failure(new
				ConflictException($"Chord: {chordDto.ChordName} already exists."));

			try
			{
				chord.ChordName = chordDto.ChordName;
				chord.Difficulty = chordDto.Difficulty;
				chord.ChordType = chordDto.ChordType;

                await _context.SaveChangesAsync();

                var updatedChord = chord.Adapt<Chord, ChordEditDto>();
                return ServiceResult<ChordEditDto>.Success(updatedChord);
            }
			catch (Exception ex)
			{
                _logger.LogError(ex, "Error updating chord: {ChordName}. Error: {Error}", chordDto.ChordName, ex);
                return ServiceResult<ChordEditDto>.Failure(new Exception(ex.Message));
			}			
		}

		public async Task<ServiceResult<bool>> DeleteChordAsync(string id)
		{
			var chord = await _context.Chords.FindAsync(id);
			if (chord == null) return ServiceResult<bool>.Failure(new
				NotFoundException($"Chord with ID: {id} does not exist."));
			try
			{
				_context.Chords.Remove(chord);
				await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
			catch (Exception ex)
			{
                _logger.LogError(ex, "Error deleting chord with ID: {Id}. Error: {Error}", id, ex);
                return ServiceResult<bool>.Failure(new Exception(ex.Message));
			}
		}
	}
}
