using AutoMapper;
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
using Mapster;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class ChordService : IChordService
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

		public ChordService(SongDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<ServiceResult<List<ChordEditDto>>> GetAllChordsAsync()
		{
			var chords = await _context.Chords
								.OrderBy(c => c.ChordName)
								.ToListAsync();

			//var chordsDto = chords.Select(_mapper.Map<Chord, ChordEditDto>).ToList();

			var chordsDto = _mapper.Map<List<ChordEditDto>>(chords);

			return ServiceResult<List<ChordEditDto>>.Success(chordsDto);
		}

		public async Task<ServiceResult<List<ChordWithChartsDto>>> GetChordsWithChartsAsync()
		{
			var chords = await _context.Chords
								.OrderBy(c => c.ChordName)
								.Include(ch => ch.ChordCharts.OrderBy(cc => cc.FretPosition))
								.ToListAsync();

			var chordsDto = _mapper.Map<List<ChordWithChartsDto>>(chords);

			return ServiceResult<List<ChordWithChartsDto>>.Success(chordsDto);
		}

		public async Task<ServiceResult<ChordEditDto>> GetChordByIdAsync(long id)
		{
			var chord = await _context.Chords.FindAsync(id);

			if (chord == null) return ServiceResult<ChordEditDto>.Failure(new
				NotFoundException($"Chord with ID: {id} does not exist."));

			var chordDto = _mapper.Map<Chord, ChordEditDto>(chord);

			return ServiceResult<ChordEditDto>.Success(chordDto);
		}

		public async Task<ServiceResult<ChordWithChartsDto>> GetChordWithChartsByIdAsync(long id)
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

			var chordDto = _mapper.Map<Chord, ChordWithChartsDto>(chord);

			return ServiceResult<ChordWithChartsDto>.Success(chordDto);
		}

		public async Task<ServiceResult<ChordEditDto>> CreateChordAsync(ChordCreateDto chordDto)
		{
			if (chordDto == null) return ServiceResult<ChordEditDto>.Failure(new
				BadRequestException("Chord data is Required"));

			var chordExists = await _context.Chords
							.AnyAsync(ch => ch.ChordName == chordDto.ChordName);

			if (chordExists) return ServiceResult<ChordEditDto>.Failure(new
				ConflictException($"Chord: {chordDto.ChordName} already exists."));

			var chord = _mapper.Map<ChordCreateDto, Chord>(chordDto);

			try
			{
				await _context.Chords.AddAsync(chord);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<ChordEditDto>.Failure(new Exception(ex.Message));
			}

			var newChord = _mapper.Map<Chord, ChordEditDto>(chord);

			return ServiceResult<ChordEditDto>.Success(newChord);
		}

		public async Task<ServiceResult<ChordSimpleDto>> CreateSimpleChordAsync(ChordSimpleDto chordDto)
		{
			if (chordDto == null) return ServiceResult<ChordSimpleDto>.Failure(new
				BadRequestException("Chord data is Required"));

			var chordExists = await _context.Chords
							.AnyAsync(ch => ch.ChordName == chordDto.ChordName);

			if (chordExists) return ServiceResult<ChordSimpleDto>.Failure(new
				ConflictException($"Chord: {chordDto.ChordName} already exists."));

			var chord = chordDto.Adapt<Chord>();

			try
			{
				await _context.Chords.AddAsync(chord);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<ChordSimpleDto>.Failure(new Exception(ex.Message));
			}

			var newChord = chord.Adapt<ChordSimpleDto>();

			return ServiceResult<ChordSimpleDto>.Success(newChord);
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

			_mapper.Map(chordDto, chord);
			try
			{
				chord.ChordName = chordDto.ChordName;
				chord.Difficulty = chordDto.Difficulty;
				chord.ChordType = chordDto.ChordType;

				await _context.SaveChangesAsync();

                var updatedChord = _mapper.Map<Chord, ChordEditDto>(chord);
                return ServiceResult<ChordEditDto>.Success(updatedChord);
            }
			catch (Exception ex)
			{
				return ServiceResult<ChordEditDto>.Failure(new Exception(ex.Message));
			}			
		}

		public async Task<ServiceResult<bool>> DeleteChordAsync(long id)
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
				return ServiceResult<bool>.Failure(new Exception(ex.Message));
			}
		}
	}
}
