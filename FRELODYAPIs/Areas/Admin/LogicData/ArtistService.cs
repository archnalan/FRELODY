using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class ArtistService : IArtistService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<ArtistService> _logger;

        public ArtistService(SongDbContext context, ILogger<ArtistService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get all artists
        public async Task<ServiceResult<List<ArtistDto>>> GetAllArtists()
        {
            try
            {
                var artists = await _context.Artists
                                    .OrderBy(a => a.Name)
                                    .ToListAsync();
                var artistsDto = artists.Adapt<List<ArtistDto>>();

                return ServiceResult<List<ArtistDto>>.Success(artistsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving artists.{Error}", ex);
                return ServiceResult<List<ArtistDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving artists."));
            }
        }
        #endregion

        #region Get artist by Id
        public async Task<ServiceResult<ArtistDto>> GetArtistById(string id)
        {
            try
            {
                var artist = await _context.Artists
                                    .FirstOrDefaultAsync(a => a.Id == id);
                if (artist == null)
                {
                    _logger.LogWarning("Artist with Id {Id} not found.", id);
                    return ServiceResult<ArtistDto>.Failure(
                        new NotFoundException($"Artist with Id {id} not found."));
                }
                var artistDto = artist.Adapt<ArtistDto>();
                return ServiceResult<ArtistDto>.Success(artistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving artist with Id {Id}. {Error}", id, ex);
                return ServiceResult<ArtistDto>.Failure(
                    new ServerErrorException($"An error occurred while retrieving artist with Id {id}."));
            }
        }
        #endregion

        #region Create artist
        public async Task<ServiceResult<ArtistDto>> CreateArtist(ArtistDto artistDto)
        {
            try
            {
                if (artistDto == null)
                {
                    _logger.LogWarning("Artist data is null.");
                    return ServiceResult<ArtistDto>.Failure(
                        new BadRequestException("Artist data cannot be null."));
                }
                var artist = artistDto.Adapt<Artist>();
                await _context.Artists.AddAsync(artist);
                await _context.SaveChangesAsync();
                var createdArtistDto = artist.Adapt<ArtistDto>();
                return ServiceResult<ArtistDto>.Success(createdArtistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating a new artist. {Error}", ex);
                return ServiceResult<ArtistDto>.Failure(
                    new ServerErrorException("An error occurred while creating a new artist."));
            }
        }
        #endregion

        #region Delete artist by Id
        public async Task<ServiceResult<bool>> DeleteArtist(string id)
        {
            try
            {
                var artist = await _context.Artists
                                    .FirstOrDefaultAsync(a => a.Id == id);
                if (artist == null)
                {
                    _logger.LogWarning("Artist with Id {Id} not found.", id);
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Artist with Id {id} not found."));
                }
                artist.IsDeleted = true; // Mark as deleted
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting artist with Id {Id}. {Error}", id, ex);
                return ServiceResult<bool>.Failure(
                    new ServerErrorException($"An error occurred while deleting artist with Id {id}."));
            }
        }
        #endregion
    }
}