using Mapster;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.CompositeDtos;
using FRELODYAPP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web.Http.ModelBinding;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class LyricLineService : ILyricLineService
	{
		private readonly SongDbContext _context;

		public LyricLineService(SongDbContext context)
		{
			_context = context;
		}

		public async Task<ServiceResult<List<LyricLineDto>>> GetAllLyricLinesAsync()
		{
			try
            {
                var lyricLines = await _context.LyricLines
                                .OrderBy(ll => ll.LyricLineOrder)
                                .ToListAsync();

                var lyricLineDto = lyricLines.Adapt<List<LyricLineDto>>();

                return ServiceResult<List<LyricLineDto>>.Success(lyricLineDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<LyricLineDto>>.Failure(new
                    Exception($"Error retrieving lyric lines. Details: {ex.Message}"));
            }           
		}

		public async Task<ServiceResult<LyricLineDto>> GetLyricLineByIdAsync(string id)
		{
			try
            {
                var lyricLine = await _context.LyricLines.FindAsync(id);

                if (lyricLine == null) return ServiceResult<LyricLineDto>.Failure(new
                    NotFoundException($"Lyric Line with ID:{id} does not exist."));

                var lyricLineDto = lyricLine.Adapt<LyricLineDto>();

                return ServiceResult<LyricLineDto>.Success(lyricLineDto);

            }
            catch (Exception ex)
            {
                return ServiceResult<LyricLineDto>.Failure(new
                    Exception($"Error retrieving lyric line with ID:{id}. Details: {ex.Message}"));
            }           
		}

        public async Task<ServiceResult<LyricLineDto>> CreateVerseLineAsync(LineVerseCreateDto verselineDto)
		{
			if (verselineDto == null)
				return ServiceResult<LyricLineDto>.Failure( new 
					BadRequestException("Verse data is required."));

			if(verselineDto.VerseId == null || string.IsNullOrEmpty(verselineDto.VerseId)) 
				return ServiceResult<LyricLineDto>.Failure(new
					BadRequestException("Verse Id is required."));

			var verseExists = await _context.SongParts
								.AnyAsync(ll => ll.Id == verselineDto.VerseId);

			if (verseExists == false)
				return ServiceResult<LyricLineDto>.Failure(new
					NotFoundException($"Parent Verse Id:{verselineDto.VerseId} does not exist"));


			//No LyricOrderNumber is duplicated within the same verse
			var verseLineExists = await _context.LyricLines
										.Where(vl => vl.PartId == verselineDto.VerseId)// Filter by the same verse
										.AnyAsync(vl => vl.LyricLineOrder == verselineDto.LyricLineOrder);

			if (verseLineExists)			
				return ServiceResult<LyricLineDto>.Failure(new
					ConflictException($"Lyric line Order value:{verselineDto.LyricLineOrder} already taken."));

			var lineToAdd = verselineDto.Adapt<LyricLine>();

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

			var created = lineToAdd.Adapt<LyricLineDto>();


			return ServiceResult<LyricLineDto>.Success(created);
		}

		public async Task<ServiceResult<LyricLineDto>> EditVerseLineAsync(string id, LyricLineDto verseLineDto)
		{
			if (verseLineDto == null)
				return ServiceResult<LyricLineDto>.Failure(new
				BadRequestException("Verse data is required."));		

			if (id != verseLineDto.Id)
				return ServiceResult<LyricLineDto>.Failure(new
				BadRequestException($"Lyric lines of Ids:{id} and {verseLineDto.Id} are not the same."));

			//No LyricOrderNumber is duplicated within the same verse
			var verseLineExists = await _context.LyricLines
									.Where(vl => vl.PartId == verseLineDto.PartId)// Filter by the same verse
									.AnyAsync(vl => vl.Id != verseLineDto.Id && // Exclude the current LyricLine being edited
											  vl.LyricLineOrder == verseLineDto.LyricLineOrder);

			if (verseLineExists)
				return ServiceResult<LyricLineDto>.Failure(new
				ConflictException($"Lyric line Order value:{verseLineDto.LyricLineOrder} already taken."));

			var verseLineInDb = await _context.LyricLines.FindAsync(id);

			if (verseLineInDb == null) 
				return ServiceResult<LyricLineDto>.Failure(new
				NotFoundException($"Lyric Line of ID:{id} does not exist."));

           
			if (verseLineDto.PartId != null || string.IsNullOrEmpty(verseLineDto.PartId))
			{
				var verseExists = await _context.SongParts
									.Where(vl => vl.Id != verseLineDto.Id)
									.AnyAsync(vl => vl.Id == verseLineDto.PartId);

				if (verseExists == false)
					return ServiceResult<LyricLineDto>.Failure(new
					BadRequestException($"Parent Verse Id:{verseLineDto.PartId} does not exist"));
			}
			else
			{
				return ServiceResult<LyricLineDto>.Failure(new
					BadRequestException("Verse Id is required."));
			}

            verseLineDto.Adapt(verseLineInDb);

            try
            {
				_context.LyricLines.Update(verseLineInDb);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<LyricLineDto>.Failure(new
					Exception($"Error updating lyric line number: {verseLineDto.LyricLineOrder}. Details: {ex.Message}"));
			}

			var editedVerseLine = verseLineInDb.Adapt<LyricLineDto>();

			return ServiceResult<LyricLineDto>.Success(editedVerseLine);
		}

		public async Task<ServiceResult<bool>> DeleteLyricLineAsync(string id)
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
