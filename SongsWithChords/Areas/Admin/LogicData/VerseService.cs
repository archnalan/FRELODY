using AutoMapper;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web.Http.ModelBinding;
using FRELODYAPP.Data.Infrastructure;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class VerseService : IVerseService
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

		public VerseService(SongDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<ServiceResult<List<VerseDto>>> GetAllVersesAsync()
		{
			var verses = await _context.Verses
							.OrderBy(v => v.VerseNumber)
							.ToListAsync();

			var versesDto = _mapper.Map<List<VerseDto>>(verses).ToList();

			return ServiceResult<List<VerseDto>>.Success(versesDto);
		}

		public async Task<ServiceResult<VerseDto>> GetVerseByIdAsync(Guid id)
		{
			var verse = await _context.Verses.FindAsync(id);

			if (verse == null) return ServiceResult<VerseDto>.Failure(
				new NotFoundException($"Verse with ID:{id} does not exist."));

			var verseDto = _mapper.Map<VerseDto>(verse);

			return ServiceResult<VerseDto>.Success(verseDto);
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

			var verse = _mapper.Map<Verse>(verseDto);
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

			var newVerseDto = _mapper.Map<VerseDto>(verse);

			return ServiceResult<VerseDto>.Success(newVerseDto);
		}

		public async Task<ServiceResult<VerseDto>> EditVerseAsync(Guid id, VerseDto verseEdit)
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

			var verse = _mapper.Map(verseEdit, verseInDb);

			try
			{
				_context.Verses.Update(verse);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<VerseDto>.Failure(new
				Exception (ex.Message));
			}

			var newVerseDto = _mapper.Map<VerseDto>(verse);

			return ServiceResult<VerseDto>.Success(newVerseDto);
		}

		public async Task<ServiceResult<bool>> DeleteVerseAsync(Guid id)
		{
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
