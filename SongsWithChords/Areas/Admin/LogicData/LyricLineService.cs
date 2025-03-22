using AutoMapper;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.CompositeDtos;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web.Http.ModelBinding;
using FRELODYAPP.Data.Infrastructure;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class LyricLineService : ILyricLineService
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

		public LyricLineService(SongDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<ServiceResult<List<LyricLineDto>>> GetAllLyricLinesAsync()
		{
			var lyricLines = await _context.LyricLines
								.OrderBy(ll => ll.LyricLineOrder)
								.ToListAsync();

			var lyricLineDto = _mapper.Map<List<LyricLineDto>>(lyricLines);

			return ServiceResult<List<LyricLineDto>>.Success(lyricLineDto);
		}

		public async Task<ServiceResult<LyricLineDto>> GetLyricLineByIdAsync(Guid id)
		{
			var lyricLine = await _context.LyricLines.FindAsync(id);

			if (lyricLine == null) return ServiceResult<LyricLineDto>.Failure(new
				NotFoundException($"Lyric Line with ID:{id} does not exist."));

			var lyricLineDto = _mapper.Map<LyricLineDto>(lyricLine);

			return ServiceResult<LyricLineDto>.Success(lyricLineDto);
		}

        public async Task<ServiceResult<LyricLineDto>> CreateVerseLineAsync(LineVerseCreateDto verselineDto)
		{
			if (verselineDto == null)
				return ServiceResult<LyricLineDto>.Failure( new 
					BadRequestException("Verse data is required."));

			if(verselineDto.VerseId == null || verselineDto.VerseId != Guid.Empty) 
				return ServiceResult<LyricLineDto>.Failure(new
					BadRequestException("Verse Id is required."));

			var verseExists = await _context.Verses
								.AnyAsync(ll => ll.Id == verselineDto.VerseId);

			if (verseExists == false)
				return ServiceResult<LyricLineDto>.Failure(new
					NotFoundException($"Parent Verse Id:{verselineDto.VerseId} does not exist"));


			//No LyricOrderNumber is duplicated within the same verse
			var verseLineExists = await _context.LyricLines
										.Where(vl => vl.VerseId == verselineDto.VerseId)// Filter by the same verse
										.AnyAsync(vl => vl.LyricLineOrder == verselineDto.LyricLineOrder);

			if (verseLineExists)			
				return ServiceResult<LyricLineDto>.Failure(new
					ConflictException($"Lyric line Order value:{verselineDto.LyricLineOrder} already taken."));

			var lineToAdd = _mapper.Map<LyricLine>(verselineDto);

			try
			{
				_context.LyricLines.Add(lineToAdd);

				await _context.SaveChangesAsync();

			}
			catch (Exception ex)
			{
				return ServiceResult<LyricLineDto>.Failure(new
					Exception($"Error creating lyric line number: {verselineDto.LyricLineOrder}. Details: {ex.Message}"));
			}

			var created = _mapper.Map < LyricLineDto > (lineToAdd);


			return ServiceResult<LyricLineDto>.Success(created);
		}

		public async Task<ServiceResult<LyricLineDto>> EditVerseLineAsync(Guid id, LyricLineDto verseLineDto)
		{
			if (verseLineDto == null)
				return ServiceResult<LyricLineDto>.Failure(new
				BadRequestException("Verse data is required."));		

			if (id != verseLineDto.Id)
				return ServiceResult<LyricLineDto>.Failure(new
				BadRequestException($"Lyric lines of Ids:{id} and {verseLineDto.Id} are not the same."));

			//No LyricOrderNumber is duplicated within the same verse
			var verseLineExists = await _context.LyricLines
									.Where(vl => vl.VerseId == verseLineDto.VerseId)// Filter by the same verse
									.AnyAsync(vl => vl.Id != verseLineDto.Id && // Exclude the current LyricLine being edited
											  vl.LyricLineOrder == verseLineDto.LyricLineOrder);

			if (verseLineExists)
				return ServiceResult<LyricLineDto>.Failure(new
				ConflictException($"Lyric line Order value:{verseLineDto.LyricLineOrder} already taken."));

			var verseLineInDb = await _context.LyricLines.FindAsync(id);

			if (verseLineInDb == null) 
				return ServiceResult<LyricLineDto>.Failure(new
				NotFoundException($"Lyric Line of ID:{id} does not exist."));

			var verseLine = _mapper.Map(verseLineDto, verseLineInDb);

			if (verseLineDto.VerseId != null || verseLineDto.VerseId != Guid.Empty)
			{
				var verseExists = await _context.Verses
									.Where(vl => vl.Id != verseLineDto.Id)
									.AnyAsync(vl => vl.Id == verseLineDto.VerseId);

				if (verseExists == false)
					return ServiceResult<LyricLineDto>.Failure(new
					BadRequestException($"Parent Verse Id:{verseLineDto.VerseId} does not exist"));
			}
			else
			{
				return ServiceResult<LyricLineDto>.Failure(new
					BadRequestException("Verse Id is required."));
			}

			try
			{
				_context.LyricLines.Update(verseLine);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<LyricLineDto>.Failure(new
					Exception($"Error updating lyric line number: {verseLineDto.LyricLineOrder}. Details: {ex.Message}"));
			}

			var editedVerseLine = _mapper.Map<LyricLineDto>(verseLine);

			return ServiceResult<LyricLineDto>.Success(editedVerseLine);
		}

		public async Task<ServiceResult<bool>> DeleteLyricLineAsync(Guid id)
		{
			var lyricLine = await _context.LyricLines.FindAsync(id);

			if (lyricLine == null) return ServiceResult<bool>.Failure(new
				NotFoundException($"Lyric line of ID:{id} does not exist."));

			try
			{
				_context.LyricLines.Remove(lyricLine);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<bool>.Failure(new 
					Exception($"Error deleting lyric line number: {lyricLine.LyricLineOrder}. Details: {ex.Message}"));
			}

			return ServiceResult<bool>.Success(true);
		}
	}
}
