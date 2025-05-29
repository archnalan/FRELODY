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
    public class VerseService : IVerseService
	{
		private readonly SongDbContext _context;

		public VerseService(SongDbContext context)
		{
			_context = context;
		}

		public async Task<ServiceResult<List<VerseDto>>> GetAllVersesAsync()
		{
			try
            {
                var verses = await _context.Verses
                            .OrderBy(v => v.VerseNumber)
                            .ToListAsync();

                var versesDto = verses.Adapt<List<VerseDto>>();

                return ServiceResult<List<VerseDto>>.Success(versesDto);

            }
            catch (Exception ex)
            {
                return ServiceResult<List<VerseDto>>.Failure(new
                    Exception($"An error occurred while retrieving verses: {ex.Message}"));
            }
           
		}

		public async Task<ServiceResult<VerseDto>> GetVerseByIdAsync(string id)
		{
			try
			{
                var verse = await _context.Verses.FindAsync(id);

                if (verse == null) return ServiceResult<VerseDto>.Failure(
                    new NotFoundException($"Verse with ID:{id} does not exist."));

                var verseDto = verse.Adapt<VerseDto>();

                return ServiceResult<VerseDto>.Success(verseDto);
            }
			catch (Exception ex) 
			{
                return ServiceResult<VerseDto>.Failure(new
                    Exception($"An error occurred while retrieving verse with ID:{id}. {ex.Message}"));
            }
			
		}		

		public async Task<ServiceResult<VerseDto>> CreateVerseAsync(VerseCreateDto verseDto)
		{
            if (verseDto == null) return ServiceResult<VerseDto>.Failure( new
				BadRequestException("Verse data is required."));		

			var SongInDb = await _context.Songs.FindAsync(verseDto.SongId);

			if (SongInDb == null)
				return ServiceResult<VerseDto>.Failure(new
				BadRequestException($"Parent Song with ID: {verseDto.SongId} does not exist"));

			var verseExists = await _context.Verses
								.Where(v => v.SongId == verseDto.SongId)
								.AnyAsync(v => v.VerseNumber == verseDto.VerseNumber);

			if (verseExists)
				return ServiceResult<VerseDto>.Failure(new
				ConflictException($"Verse Number {verseDto.VerseNumber} already exists for this Song"));

			var verse = verseDto.Adapt<Verse>();
			try
			{
				await _context.Verses.AddAsync(verse);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<VerseDto>.Failure(new
				Exception(ex.Message));
			}

			var newVerseDto = verse.Adapt<VerseDto>();

			return ServiceResult<VerseDto>.Success(newVerseDto);
		}

		public async Task<ServiceResult<VerseDto>> EditVerseAsync(string id, VerseDto verseEdit)
		{
			if (verseEdit == null) return ServiceResult<VerseDto>.Failure(new
				BadRequestException("Verse data is required."));
			
			if (id != verseEdit.Id)
				return ServiceResult<VerseDto>.Failure(new
				BadRequestException($"Invalid Attempt! Verses of IDs:{id} and {verseEdit.Id} are not the same"));

			var verseExists = await _context.Verses
								.Where(v => v.Id != id)
								.AnyAsync(v => v.SongId == verseEdit.SongId && v.VerseNumber == verseEdit.VerseNumber);

			if (verseExists)
			{
				return ServiceResult<VerseDto>.Failure(new
				ConflictException($"Verse Number {verseEdit.VerseNumber} already exists for this Song"));
			}

			var verseInDb = await _context.Verses.FindAsync(id);

			if (verseInDb == null)
				return ServiceResult<VerseDto>.Failure(new
				BadRequestException($"Verse with ID: {id} does not exist"));

			var SongInDb = await _context.Songs.FindAsync(verseEdit.SongId);

			if (SongInDb == null)
				return ServiceResult<VerseDto>.Failure(new
				BadRequestException($"Parent Song with ID: {verseEdit.SongId} does not exist"));
			
			verseEdit.Adapt(verseInDb);

			try
			{
				_context.Verses.Update(verseInDb);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<VerseDto>.Failure(new
				Exception (ex.Message));
			}

			var newVerseDto = verseInDb.Adapt<VerseDto>();

			return ServiceResult<VerseDto>.Success(newVerseDto);
		}

		public async Task<ServiceResult<bool>> DeleteVerseAsync(string id)
		{
            if (string.IsNullOrEmpty(id)) return ServiceResult<bool>.Failure(new
                BadRequestException("Verse ID is required."));

            var verse = await _context.Verses.FindAsync(id);

			if (verse == null) return ServiceResult<bool>.Failure(new
				NotFoundException($"Verse with ID:{id} does not exist."));

			try
			{
				_context.Verses.Remove(verse);
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
