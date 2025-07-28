using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web.Http.ModelBinding;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;
using Mapster;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class SongPartService : ISongPartService
	{
		private readonly SongDbContext _context;

		public SongPartService(SongDbContext context)
		{
			_context = context;
		}

		public async Task<ServiceResult<List<SongPartDto>>> GetAllVersesAsync()
		{
			try
            {
                var verses = await _context.SongParts
                            .OrderBy(v => v.PartNumber)
                            .ToListAsync();

                var versesDto = verses.Adapt<List<SongPartDto>>();

                return ServiceResult<List<SongPartDto>>.Success(versesDto);

            }
            catch (Exception ex)
            {
                return ServiceResult<List<SongPartDto>>.Failure(new
                    Exception($"An error occurred while retrieving verses: {ex.Message}"));
            }
           
		}

		public async Task<ServiceResult<SongPartDto>> GetVerseByIdAsync(string id)
		{
			try
			{
                var verse = await _context.SongParts.FindAsync(id);

                if (verse == null) return ServiceResult<SongPartDto>.Failure(
                    new NotFoundException($"Verse with ID:{id} does not exist."));

                var verseDto = verse.Adapt<SongPartDto>();

                return ServiceResult<SongPartDto>.Success(verseDto);
            }
			catch (Exception ex) 
			{
                return ServiceResult<SongPartDto>.Failure(new
                    Exception($"An error occurred while retrieving verse with ID:{id}. {ex.Message}"));
            }
			
		}		

		public async Task<ServiceResult<SongPartDto>> CreateVerseAsync(VerseCreateDto verseDto)
		{
            if (verseDto == null) return ServiceResult<SongPartDto>.Failure( new
				BadRequestException("Verse data is required."));		

			var SongInDb = await _context.Songs.FindAsync(verseDto.SongId);

			if (SongInDb == null)
				return ServiceResult<SongPartDto>.Failure(new
				BadRequestException($"Parent Song with ID: {verseDto.SongId} does not exist"));

			var verseExists = await _context.SongParts
								.Where(v => v.SongId == verseDto.SongId)
								.AnyAsync(v => v.PartNumber == verseDto.VerseNumber);

			if (verseExists)
				return ServiceResult<SongPartDto>.Failure(new
				ConflictException($"Verse Number {verseDto.VerseNumber} already exists for this Song"));

			var verse = verseDto.Adapt<SongPart>();
			try
			{
				await _context.SongParts.AddAsync(verse);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<SongPartDto>.Failure(new
				Exception(ex.Message));
			}

			var newVerseDto = verse.Adapt<SongPartDto>();

			return ServiceResult<SongPartDto>.Success(newVerseDto);
		}

		public async Task<ServiceResult<SongPartDto>> EditVerseAsync(string id, SongPartDto verseEdit)
		{
			if (verseEdit == null) return ServiceResult<SongPartDto>.Failure(new
				BadRequestException("Verse data is required."));
			
			if (id != verseEdit.Id)
				return ServiceResult<SongPartDto>.Failure(new
				BadRequestException($"Invalid Attempt! Verses of IDs:{id} and {verseEdit.Id} are not the same"));

			var verseExists = await _context.SongParts
								.Where(v => v.Id != id)
								.AnyAsync(v => v.SongId == verseEdit.SongId && v.PartNumber == verseEdit.PartNumber);

			if (verseExists)
			{
				return ServiceResult<SongPartDto>.Failure(new
				ConflictException($"Verse Number {verseEdit.PartNumber} already exists for this Song"));
			}

			var verseInDb = await _context.SongParts.FindAsync(id);

			if (verseInDb == null)
				return ServiceResult<SongPartDto>.Failure(new
				BadRequestException($"Verse with ID: {id} does not exist"));

			var SongInDb = await _context.Songs.FindAsync(verseEdit.SongId);

			if (SongInDb == null)
				return ServiceResult<SongPartDto>.Failure(new
				BadRequestException($"Parent Song with ID: {verseEdit.SongId} does not exist"));
			
			verseEdit.Adapt(verseInDb);

			try
			{
				_context.SongParts.Update(verseInDb);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<SongPartDto>.Failure(new
				Exception (ex.Message));
			}

			var newVerseDto = verseInDb.Adapt<SongPartDto>();

			return ServiceResult<SongPartDto>.Success(newVerseDto);
		}

		public async Task<ServiceResult<bool>> DeleteVerseAsync(string id)
		{
            if (string.IsNullOrEmpty(id)) return ServiceResult<bool>.Failure(new
                BadRequestException("Verse ID is required."));

            var verse = await _context.SongParts.FindAsync(id);

			if (verse == null) return ServiceResult<bool>.Failure(new
				NotFoundException($"Verse with ID:{id} does not exist."));

			try
			{
				_context.SongParts.Remove(verse);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<bool>.Failure(new
				Exception(ex.Message));
			}

			return ServiceResult<bool>.Success(true);
		}

	}
}
