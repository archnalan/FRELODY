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
    public class LyricSegmentService : ILyricSegment
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;
		public LyricSegmentService(SongDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<ServiceResult<List<LyricSegmentDto>>> GetAllSegmentsAsync()
		{
			var lyricSegments = await _context.LyricSegments
							.OrderBy(ch => ch.LyricOrder)
							.ToListAsync();

			var lyricSegmentsDto = _mapper.Map<List<LyricSegmentDto>>(lyricSegments);

			return ServiceResult<List<LyricSegmentDto>>.Success(lyricSegmentsDto);
		}

		public async Task<ServiceResult<LyricSegmentDto>> GetSegmentByIdAsync(Guid id)
		{
			var lyricSegment = await _context.LyricSegments.FindAsync(id);

			if (lyricSegment == null) return ServiceResult<LyricSegmentDto>.Failure(new
				NotFoundException($"Lyric Segment with ID:{id} does not exist."));

			var lyricSegmentDto = _mapper.Map<LyricSegmentDto>(lyricSegment);

			return ServiceResult<LyricSegmentDto>.Success(lyricSegmentDto);
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

			var lyricSegment = _mapper.Map<LyricSegment>(segmentDto);
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

			var newLyricSegmentDto = _mapper.Map<LyricSegmentDto>(lyricSegment);

			return ServiceResult<LyricSegmentDto>.Success(newLyricSegmentDto);
		}

		public async Task<ServiceResult<LyricSegmentDto>> EditSegmentAsyc(Guid id, LyricSegmentDto segmentDto)
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

			var segment = _mapper.Map(segmentDto, segmentToEdit);

			try
			{
				_context.LyricSegments.Update(segment);
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

			var editedSegmentDto = _mapper.Map<LyricSegmentDto>(segment);

			return ServiceResult<LyricSegmentDto>.Success(editedSegmentDto);
		}

		public async Task<ServiceResult<bool>> DeleteSegmentAsync(Guid id)
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
