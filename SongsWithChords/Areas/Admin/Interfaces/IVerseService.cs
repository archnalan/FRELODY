﻿using SongsWithChords.Dtos;
using SongsWithChords.ServiceHandler;

namespace SongsWithChords.Areas.Admin.Interfaces
{
	public interface IVerseService
	{
		Task<ServiceResult<List<VerseDto>>> GetAllVersesAsync();

		Task<ServiceResult<VerseDto>> GetVerseByIdAsync(Guid id);

		Task<ServiceResult<VerseDto>> CreateVerseAsync(VerseCreateDto verseDto);

		Task<ServiceResult<VerseDto>> EditVerseAsync(Guid id, VerseDto verseEdit);

		Task<ServiceResult<bool>> DeleteVerseAsync(Guid id);
	}
}
