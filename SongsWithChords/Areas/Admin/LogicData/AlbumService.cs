using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class AlbumService : IAlbumService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<AlbumService> _logger;

        public AlbumService(SongDbContext context, ILogger<AlbumService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get all albums
        public async Task<ServiceResult<List<AlbumDto>>> GetAllAlbums()
        {
            try
            {
                var albums = await _context.Albums
                    .OrderBy(a => a.ReleaseDate)
                    .ThenBy(a => a.Title)
                    .ToListAsync();

                var albumsDto = albums.Adapt<List<AlbumDto>>();
                return ServiceResult<List<AlbumDto>>.Success(albumsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving albums. {Error}", ex);
                return ServiceResult<List<AlbumDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving albums."));
            }
        }
        #endregion

        #region Get Category By Id
        public async Task<ServiceResult<AlbumDto>> GetAlbumById(string albumId)
        {
            try
            {
                if (string.IsNullOrEmpty(albumId))
                {
                    return ServiceResult<AlbumDto>.Failure(
                        new BadRequestException("Album ID is required"));
                }
                var album = await _context.Albums.FindAsync(albumId);
                if (album == null)
                {
                    return ServiceResult<AlbumDto>.Failure(
                        new NotFoundException($"Album not found. ID: {albumId}"));
                }
                var albumDto = album.Adapt<AlbumDto>();
                return ServiceResult<AlbumDto>.Success(albumDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving album {AlbumId}. {Error}", albumId, ex);
                return ServiceResult<AlbumDto>.Failure(
                    new ServerErrorException("An error occurred while retrieving the specified album."));
            }
        }
        #endregion

        #region Get albums by artist Id
        public async Task<ServiceResult<List<AlbumDto>>> GetAlbumsByArtistId(string artistId)
        {
            try
            {
                if (string.IsNullOrEmpty(artistId))
                {
                    return ServiceResult<List<AlbumDto>>.Failure(
                        new BadRequestException("Artist ID is required"));
                }

                var albums = await _context.Albums
                    .Where(a => a.ArtistId == artistId)
                    .OrderBy(a => a.ReleaseDate)
                    .ThenBy(a => a.Title)
                    .ToListAsync();

                var albumsDto = albums.Adapt<List<AlbumDto>>();
                return ServiceResult<List<AlbumDto>>.Success(albumsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving albums for artist {ArtistId}. {Error}", artistId, ex);
                return ServiceResult<List<AlbumDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving albums for the specified artist."));
            }
        }
        #endregion

        #region Get all songs by album
        public async Task<ServiceResult<List<SongDto>>> GetAllSongsByAlbumId(string albumId)
        {
            try
            {
                if (string.IsNullOrEmpty(albumId))
                {
                    return ServiceResult<List<SongDto>>.Failure(
                        new BadRequestException("Album ID is required"));
                }

                var songs = await _context.Songs
                    .Where(s => s.AlbumId == albumId)
                    .OrderBy(s => s.SongNumber)
                    .ThenBy(s => s.Title)
                    .ToListAsync();

                var songsDto = songs.Adapt<List<SongDto>>();
                return ServiceResult<List<SongDto>>.Success(songsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving songs for album {AlbumId}. {Error}", albumId, ex);
                return ServiceResult<List<SongDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving songs for the specified album."));
            }
        }
        #endregion

        #region Create an album
        public async Task<ServiceResult<AlbumDto>> CreateAlbum(AlbumDto albumDto)
        {
            try
            {
                if (albumDto == null)
                {
                    return ServiceResult<AlbumDto>.Failure(
                        new BadRequestException("Album data is required"));
                }

                if (!string.IsNullOrEmpty(albumDto.ArtistId))
                {
                    var artistExists = await _context.Artists.AnyAsync(a => a.Id == albumDto.ArtistId);
                    if (!artistExists)
                    {
                        return ServiceResult<AlbumDto>.Failure(
                            new BadRequestException($"Artist does not exist. ID:{albumDto.ArtistId} "));
                    }
                }

                var album = albumDto.Adapt<Album>();
                album.Id = Guid.NewGuid().ToString();

                _context.Albums.Add(album);
                await _context.SaveChangesAsync();

                var createdAlbumDto = album.Adapt<AlbumDto>();
                return ServiceResult<AlbumDto>.Success(createdAlbumDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating an album. {Error}", ex);
                return ServiceResult<AlbumDto>.Failure(
                    new ServerErrorException("An error occurred while creating the album."));
            }
        }
        #endregion

        #region Update an album
        public async Task<ServiceResult<AlbumDto>> UpdateAlbum(string albumId, AlbumDto albumDto)
        {
            try
            {
                if (string.IsNullOrEmpty(albumId) || albumDto == null)
                {
                    return ServiceResult<AlbumDto>.Failure(
                        new BadRequestException("Album ID and data are required"));
                }

                var existingAlbum = await _context.Albums.FindAsync(albumId);
                if (existingAlbum == null)
                {
                    return ServiceResult<AlbumDto>.Failure(
                        new NotFoundException($"Album not found. ID: {albumId}"));
                }

                if (!string.IsNullOrEmpty(albumDto.ArtistId))
                {
                    var artistExists = await _context.Artists.AnyAsync(a => a.Id == albumDto.ArtistId);
                    if (!artistExists)
                    {
                        return ServiceResult<AlbumDto>.Failure(
                            new BadRequestException($"Artist does not exist. ID:{albumDto.ArtistId} "));
                    }
                }

                existingAlbum.Title = albumDto.Title;
                existingAlbum.Slug = albumDto.Slug;
                existingAlbum.ReleaseDate = albumDto.ReleaseDate;
                existingAlbum.Label = albumDto.Label;
                existingAlbum.ArtistId = albumDto.ArtistId;

                _context.Albums.Update(existingAlbum);
                await _context.SaveChangesAsync();

                var updatedAlbumDto = existingAlbum.Adapt<AlbumDto>();
                return ServiceResult<AlbumDto>.Success(updatedAlbumDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while updating album {AlbumId}. {Error}", albumId, ex);
                return ServiceResult<AlbumDto>.Failure(
                    new ServerErrorException("An error occurred while updating the album."));
            }
        }

        #endregion
    }
}