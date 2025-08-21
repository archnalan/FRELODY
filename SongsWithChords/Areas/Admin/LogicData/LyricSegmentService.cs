using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web.Http.ModelBinding;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;
using Mapster;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public class LyricSegmentService : ILyricSegment
	{
		private readonly SongDbContext _context;
		public LyricSegmentService(SongDbContext context)
		{
			_context = context;
		}

		public async Task<ServiceResult<List<LyricSegmentDto>>> GetAllSegmentsAsync()
		{
			var lyricSegments = await _context.LyricSegments
							.OrderBy(ch => ch.LyricOrder)
							.ToListAsync();

			var lyricSegmentsDto = lyricSegments.Adapt<List<LyricSegmentDto>>();

			return ServiceResult<List<LyricSegmentDto>>.Success(lyricSegmentsDto);
		}

		public async Task<ServiceResult<LyricSegmentDto>> GetSegmentByIdAsync(string id)
		{
           
			try
            {
                if (string.IsNullOrEmpty(id))
                    return ServiceResult<LyricSegmentDto>.Failure(new
                        BadRequestException("Lyric Segment ID is required."));

                var lyricSegment = await _context.LyricSegments.FindAsync(id);

                if (lyricSegment == null) return ServiceResult<LyricSegmentDto>.Failure(new
                    NotFoundException($"Lyric Segment with ID:{id} does not exist."));

                var lyricSegmentDto = lyricSegment.Adapt<LyricSegmentDto>();

                return ServiceResult<LyricSegmentDto>.Success(lyricSegmentDto);
            }
			catch (Exception ex) 
			{
				return ServiceResult<LyricSegmentDto>.Failure(new
					ServerErrorException($"Error retrieving segment with ID: {id}. Details: {ex.Message}"));
            }           
		}

		public async Task<ServiceResult<LyricSegmentDto>> CreateSegmentAsync(LyricSegmentCreateDto segmentDto)
		{
			if (segmentDto == null)
				return ServiceResult<LyricSegmentDto>.Failure(new
					BadRequestException("Lyric Segment data is required."));			

			var LyricLineExists = await _context.LyricLines
									.AnyAsync(ll => ll.Id == segmentDto.LyricLineId);

			if (LyricLineExists == false)
				return ServiceResult<LyricSegmentDto>.Failure(new
					BadRequestException($"Lyric Line with ID:{segmentDto.LyricLineId} does not exist."));

			//Avoid repetition of Lyric Segment Order values in same line
			var segmentExists = await _context.LyricSegments
								.Where(ls => ls.LyricLineId == segmentDto.LyricLineId)
								.AnyAsync(ls => ls.LyricOrder == segmentDto.LyricOrder);

			if (segmentExists)
				return ServiceResult<LyricSegmentDto>.Failure(new
					ConflictException($"Invalid! Lyric with same OrderNo:{segmentDto.LyricOrder} already exists on this Lyric Line"));

			var lyricSegment = segmentDto.Adapt<LyricSegment>();
			try
			{
				_context.LyricSegments.Add(lyricSegment);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return ServiceResult<LyricSegmentDto>.Failure(new
					Exception($"Error Creating segment: {lyricSegment.Lyric}. Details:{ex.Message}"));
			}

			var newLyricSegmentDto = lyricSegment.Adapt<LyricSegmentDto>();

			return ServiceResult<LyricSegmentDto>.Success(newLyricSegmentDto);
		}

		public async Task<ServiceResult<LyricSegmentDto>> EditSegmentAsyc(string id, LyricSegmentDto segmentDto)
		{
			if (segmentDto == null) return ServiceResult<LyricSegmentDto>.Failure(new
				BadRequestException("Lyric Segment data is required."));		

			if (id != segmentDto.Id)
				return ServiceResult<LyricSegmentDto>.Failure(new
				BadRequestException($"Segments of IDs:{id} and {segmentDto.Id} are not the same."));

			var lyricLineExists = await _context.LyricLines
									.AnyAsync(ll => ll.Id == segmentDto.LyricLineId);

			if (lyricLineExists == false)
				return ServiceResult<LyricSegmentDto>.Failure(new
				NotFoundException($"Lyric Line with ID:{segmentDto.LyricLineId} does not exist."));

			//Avoid repetition of Lyric Segment Order values in same line
			var segmentExists = await _context.LyricSegments
								.Where(ls => ls.LyricLineId == segmentDto.LyricLineId)
								.AnyAsync(ls => ls.Id != id && ls.LyricOrder == segmentDto.LyricOrder);

			if (segmentExists)
				return ServiceResult<LyricSegmentDto>.Failure(new
				ConflictException($"Invalid! Lyric with same OrderNo:{segmentDto.LyricOrder} already exists on this Lyric Line"));

			var segmentToEdit = await _context.LyricSegments.FindAsync(id);

			if (segmentToEdit == null) return ServiceResult<LyricSegmentDto>.Failure(new
				NotFoundException($"Lyric of ID:{id} does not exist"));

            segmentDto.Adapt(segmentToEdit);

			try
			{
				_context.LyricSegments.Update(segmentToEdit);
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateException dbEx)
			{
				return ServiceResult<LyricSegmentDto>.Failure(new
				BadRequestException($"Error updating segment: {segmentDto.Lyric} Database details: {dbEx.Message}"));			
			}
			catch (Exception ex)
			{
				return ServiceResult<LyricSegmentDto>.Failure(new
				Exception($"Error updating lyric segment: {segmentDto.Lyric}. Details: {ex.Message}"));
			}

			var editedSegmentDto = segmentToEdit.Adapt<LyricSegmentDto>();

			return ServiceResult<LyricSegmentDto>.Success(editedSegmentDto);
		}

		public async Task<ServiceResult<bool>> DeleteSegmentAsync(string id)
		{
			var segment = await _context.LyricSegments.FindAsync(id);

			if (segment == null)
				return ServiceResult<bool>.Failure(new
					NotFoundException($"Lyric Segment with ID: {id} does not exist."));

			try
			{
				_context.LyricSegments.Remove(segment);

				await _context.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				if (ex.InnerException is
					Microsoft.Data.SqlClient.SqlException sqlEx
					&& sqlEx.Number == 547)
					return ServiceResult<bool>.Failure(new
					BadRequestException(($"Cannot delete Lyric Segment with ID: {id} due to related data. {sqlEx.Message}")));

				return ServiceResult<bool>.Failure(new
					BadRequestException($"Error deleting segment with ID: {id} Database details:{ex.Message}"));
			}
			catch (Exception ex)
			{
				return ServiceResult<bool>.Failure(new
					Exception($"Error deleting segment with ID: {id} Details:{ex.Message}"));
			}

			return ServiceResult<bool>.Success(true);
		}
	}
}
