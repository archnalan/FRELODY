using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class SongBookService : ISongBookService
    {

        private readonly SongDbContext _context;
        private readonly ILogger<SongBookService> _logger;
        public SongBookService(SongDbContext context, ILogger<SongBookService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get all song books
        public async Task<ServiceResult<List<SongBookDto>>> GetAllSongBooks()
        {
            try
            {
                var SongBooks = await _context.SongBooks
                                    .OrderBy(hb => hb.Title)
                                    .ToListAsync();
                var songBooksDto = SongBooks.Adapt<List<SongBookDto>>();

                return ServiceResult<List<SongBookDto>>.Success(songBooksDto);

            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving song books.{Error}", ex);
                return ServiceResult<List<SongBookDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving song books."));
            }
        }

        #endregion

        #region Get song book by Id
        public async Task<ServiceResult<SongBookDto>> GetSongBookById(string id)
        {
            try
            {
                var songBook = await _context.SongBooks
                                    .FirstOrDefaultAsync(hb => hb.Id == id);
                if (songBook == null)
                {
                    _logger.LogWarning("Song book with Id {Id} not found.", id);
                    return ServiceResult<SongBookDto>.Failure(
                        new NotFoundException($"Song book with Id {id} not found."));
                }
                var songBookDto = songBook.Adapt<SongBookDto>();
                return ServiceResult<SongBookDto>.Success(songBookDto);

            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving song book with Id {Id}. {Error}", id, ex);
                return ServiceResult<SongBookDto>.Failure(
                    new ServerErrorException($"An error occurred while retrieving song book with Id {id}."));
            }
        }
        #endregion

        #region Create song book
        public async Task<ServiceResult<SongBookDto>> CreateSongBook(SongBookDto songBookDto)
        {
            try
            {
                if (songBookDto == null)
                {
                    _logger.LogWarning("Song book data is null.");
                    return ServiceResult<SongBookDto>.Failure(
                        new BadRequestException("Song book data cannot be null."));
                }
                var songBook = songBookDto.Adapt<SongBook>();
                await _context.SongBooks.AddAsync(songBook);
                await _context.SaveChangesAsync();
                var createdSongBookDto = songBook.Adapt<SongBookDto>();
                return ServiceResult<SongBookDto>.Success(createdSongBookDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating a new song book. {Error}", ex);
                return ServiceResult<SongBookDto>.Failure(
                    new ServerErrorException("An error occurred while creating a new song book."));
            }
        }
        #endregion


        #region Delete song book by Id
        public async Task<ServiceResult<bool>> DeleteSongBook(string id)
        {
            try
            {
                var songBook = await _context.SongBooks
                                    .FirstOrDefaultAsync(hb => hb.Id == id);
                if (songBook == null)
                {
                    _logger.LogWarning("Song book with Id {Id} not found.", id);
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Song book with Id {id} not found."));
                }
                songBook.IsDeleted = true; // Mark as deleted
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting song book with Id {Id}. {Error}", id, ex);
                return ServiceResult<bool>.Failure(
                    new ServerErrorException($"An error occurred while deleting song book with Id {id}."));
            }
        }
        #endregion
    }
}
