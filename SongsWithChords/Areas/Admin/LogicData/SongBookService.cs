using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;
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
    }
}
