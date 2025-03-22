using AutoMapper;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.Linq;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPP.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
	[ApiController]
	[Area("Admin")]
	public class Api_LyricSegmentsController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

		public Api_LyricSegmentsController(SongDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		//GET admin/api_lyricsegments
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var lyricSegments = await _context.LyricSegments
							.OrderBy(ch => ch.LyricOrder)
							.ToListAsync();

			var lyricSegmentsDto = _mapper.Map<List<LyricSegmentDto>>(lyricSegments);

			return Ok(lyricSegmentsDto);
		}

		//GET admin/api_lyricsegments/chords
		[HttpGet("chords")]
		public async Task<IActionResult> GetSegmentsWithChords()
		{
			var lyricSegments = await _context.LyricSegments
							.Include(ch => ch.Chord)
							.OrderBy(ch => ch.LyricOrder)
							.ToListAsync();

			if (lyricSegments == null || lyricSegments.Any() == false) 
				return NotFound("No Lyric with chords added yet!");

			var lyricSegmentDto = lyricSegments.Select(_mapper.Map<LyricSegment, LyricSegmentDto>)
											.ToList();
			return Ok(lyricSegmentDto);
		}

		//GET admin/api_lyricsegments/5

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSegmentById(int id)
		{
			var lyricSegment = await _context.LyricSegments.FindAsync(id);

			if (lyricSegment == null) return NotFound($"Lyric Segment with ID:{id} does not exist.");

			var lyricSegmentDto = _mapper.Map<LyricSegmentDto>(lyricSegment);

			return Ok(lyricSegmentDto);
		}

		//GET admin/api_lyricsegments/by_ids
		[HttpGet("by_ids")]
		public async Task<IActionResult> GetSegmentsByIds(List<Guid> ids)
		{
			if (ids == null || ids.Count == 0)
				return BadRequest("Lyric Segment IDs required.");

			var lyricSegments = await _context.LyricSegments
								.Where(ls=>ids.Contains(ls.Id))							
								.OrderBy(ls => ls.LyricOrder)
								.ToListAsync();

			if (lyricSegments == null || lyricSegments.Any() == false)
				return NotFound("No Lyric with chords added yet!");

			var lyricSegmentDto = lyricSegments.Select(_mapper.Map<LyricSegment, LyricSegmentDto>)
											.ToList();
			return Ok(lyricSegmentDto);
		}

		//POST admin/api_lyricsegments/create
		[HttpPost("Create")]
		public async Task<IActionResult> Create(LyricSegmentCreateDto segmentDto)
		{
			if (segmentDto == null)
				return BadRequest("Lyric Segment data is required.");

			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var LyricLineExists = await _context.LyricLines
									.AnyAsync(ll=>ll.Id == segmentDto.LyricLineId);

			if (LyricLineExists == false) 
				return BadRequest($"Lyric Line with ID:{segmentDto.LyricLineId} does not exist.");

			//Avoid repetition of Lyric Segment Order values in same line
			var segmentExists = await _context.LyricSegments
								.Where(ls => ls.LyricLineId == segmentDto.LyricLineId)
								.AnyAsync(ls => ls.LyricOrder == segmentDto.LyricOrder);

			if (segmentExists)
				return Conflict($"Invalid! Lyric with same OrderNo:{segmentDto.LyricOrder} already exists on this Lyric Line");

			var lyricSegment = _mapper.Map<LyricSegment>(segmentDto);
			try
			{
				_context.LyricSegments.Add(lyricSegment);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var newLyricSegmentDto = _mapper.Map<LyricSegmentDto>(lyricSegment);

			return Ok(newLyricSegmentDto);
		}

		//POST admin/api_lyricsegments/create_many
		[HttpPost("Create_many")]
		public async Task<IActionResult> CreateSegments(List<LyricSegmentCreateDto> segmentDtos)
		{
			if (segmentDtos == null || segmentDtos.Count == 0)
				return BadRequest("Lyric Segment data is required.");

			var segmentsToAdd = new List<LyricSegment>();
			var createdSegmentsDto = new List<LyricSegmentDto>();
			
			var errors = new List<string>();
			var noRepeatOrder = new HashSet<(string Lyric, long LyricOrder)>();

			foreach(var segmentDto in segmentDtos)
			{
				if (!TryValidateModel(segmentDto))
				{
					errors.Add($"Invalid data for {segmentDto.Lyric}");
					continue;
				}
				var LyricLineExists = await _context.LyricLines
									.AnyAsync(ll => ll.Id == segmentDto.LyricLineId);

				if (LyricLineExists == false)
				{
					errors.Add($"Lyric Line with ID:{segmentDto.LyricLineId} does not exist.");
					continue;
				}					

				//Avoid repetition of Lyric Segment Order values in same line
				var segmentExists = await _context.LyricSegments
									.Where(ls => ls.LyricLineId == segmentDto.LyricLineId)
									.AnyAsync(ls => ls.LyricOrder == segmentDto.LyricOrder);

				if (segmentExists)
				{
					errors.Add($"Invalid! Lyric with same OrderNo:{segmentDto.LyricOrder} already exists on this Lyric Line");
					continue;
				}
					

				if(noRepeatOrder.Contains((segmentDto.Lyric, segmentDto.LyricOrder)))
				{
					errors.Add($"Invalid! Duplicate Segment: \"{segmentDto.Lyric}\" detected with same OrderNo.");
					continue;
				}

				var segment = _mapper.Map<LyricSegment>(segmentDto);
				
				segmentsToAdd.Add(segment);
				noRepeatOrder.Add((segment.Lyric, segment.LyricOrder));
			}
			if (errors.Any()) return BadRequest(errors);
			try
			{
				_context.LyricSegments.AddRange(segmentsToAdd);
				await _context.SaveChangesAsync();

				var segmentsAddedDto = _mapper.Map<List<LyricSegmentDto>>(segmentsToAdd);
				createdSegmentsDto.AddRange(segmentsAddedDto);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}		

			return Ok(createdSegmentsDto);
		}

		//PUT admin/api_lyricsegments/edit/5
		[HttpPut("edit/{id}")]
		public async Task<IActionResult> Edit(Guid id, LyricSegmentDto segmentDto)
		{
			if (segmentDto == null) return BadRequest("Lyric Segment data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != segmentDto.Id)
				return BadRequest($"Segments of IDs:{id} and {segmentDto.Id} are not the same.");

			var lyricLineExists = await _context.LyricLines
									.AnyAsync(ll => ll.Id == segmentDto.LyricLineId);

			if (lyricLineExists == false)
				return BadRequest($"Lyric Line with ID:{segmentDto.LyricLineId} does not exist.");

			//Avoid repetition of Lyric Segment Order values in same line
			var segmentExists = await _context.LyricSegments
								.Where(ls => ls.LyricLineId == segmentDto.LyricLineId)
								.AnyAsync(ls => ls.Id != id && ls.LyricOrder == segmentDto.LyricOrder);

			if (segmentExists)
				return Conflict($"Invalid! Lyric with same OrderNo:{segmentDto.LyricOrder} already exists on this Lyric Line");

			var segmentToEdit = await _context.LyricSegments.FindAsync(id);

			if (segmentToEdit == null) return NotFound($"Lyric of ID:{id} does not exist");

			var segment = _mapper.Map(segmentDto, segmentToEdit);

			try
			{
				_context.LyricSegments.Update(segment);
				await _context.SaveChangesAsync();
			}
			catch(DbUpdateException dbEx)
			{
				return BadRequest(dbEx.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var editedSegmentDto = _mapper.Map<LyricSegmentDto>(segment);

			return Ok(editedSegmentDto);
		}

		//PUT admin/api_lyricsegments/edit_many
		[HttpPut("edit_many")]
		public async Task<IActionResult> EditLyricSegments(List<LyricSegmentDto> lyricSegmentDtos)
		{
			if (lyricSegmentDtos == null || lyricSegmentDtos.Count == 0)
				return BadRequest("Lyric Segment data is required.");

			var segmentsToAdd = new List<LyricSegment>();
			var createdSegmentsDto = new List<LyricSegmentDto>();

			var errors = new List<string>();
			var noRepeatOrder = new HashSet<(string Lyric, long LyricOrder)>();

			foreach (var segmentDto in lyricSegmentDtos)
			{
				if (!TryValidateModel(segmentDto))
				{
					errors.Add($"Invalid data for {segmentDto.Lyric}");
					continue;
				}
				var lyricLineExists = await _context.LyricLines
									.AnyAsync(ll => ll.Id == segmentDto.LyricLineId);

				if (lyricLineExists == false)
				{
					errors.Add($"Lyric Line with ID:{segmentDto.LyricLineId} does not exist.");
					continue;
				}

				//Avoid repetition of Lyric Segment Order values in same line
				var segmentExists = await _context.LyricSegments
								.Where(ls => ls.LyricLineId == segmentDto.LyricLineId)
								.AnyAsync(ls => ls.Id != segmentDto.Id && ls.LyricOrder == segmentDto.LyricOrder);

				if (segmentExists)
				{
					errors.Add($"Invalid! Lyric with same OrderNo:{segmentDto.LyricOrder} already exists on this Lyric Line");
					continue;
				}

				if (noRepeatOrder.Contains((segmentDto.Lyric, segmentDto.LyricOrder)))
				{
					errors.Add($"Invalid! Duplicate Segment: \"{segmentDto.Lyric}\" detected with same OrderNo.");
					continue;
				}
				var segmentInDb = await _context.LyricSegments.FindAsync(segmentDto.Id);

				if (segmentInDb == null)
				{
					errors.Add($"Segment with ID: {segmentDto.Id} does not exist");
					continue;
				}

				var segment = _mapper.Map(segmentDto, segmentInDb);

				segmentsToAdd.Add(segment);		

				noRepeatOrder.Add((segment.Lyric, segment.LyricOrder));			

			}
			if (errors.Any()) return BadRequest(errors);
			try
			{
				_context.LyricSegments.UpdateRange(segmentsToAdd);
				await _context.SaveChangesAsync();

				var segmentsAddedDto = _mapper.Map<List<LyricSegmentDto>>(segmentsToAdd);
				createdSegmentsDto.AddRange(segmentsAddedDto);
			}
			catch (DbUpdateException dbEx)
			{
				return BadRequest(dbEx.Message);
			}			
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return Ok(createdSegmentsDto);
		}

		//DELETE admin/api_lyricsegments/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var segment = await _context.LyricSegments.FindAsync(id);

			if (segment == null) 
				return NotFound($"Lyric Segment with ID: {id} does not exist.");

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
					return BadRequest(($"Cannot delete Lyric Segment with ID: {id} due to related data. {sqlEx.Message}"));

				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return NoContent();
		}

		//DELETE admin/api_chords/by_ids
		[HttpDelete("by_ids")]		
		public async Task<IActionResult> DeleteChords(List<Guid> ids)
		{
			if (ids == null || ids.Count == 0) return BadRequest("Segments Ids required.");
			
			var segments = await _context.LyricSegments
								.Where(ls=>ids.Contains(ls.Id))
								.ToListAsync();

			if (segments.Count == 0) return NotFound("Matching chords not found for provided ids");

			var foundSegments = segments.Select(ls=>ls.Id).ToList();
			var notFoundSegments = ids.Except(foundSegments).ToList();
			
			var errors = new List<string>();

			try
			{
				_context.LyricSegments.RemoveRange(segments);
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				if (ex.InnerException is
					Microsoft.Data.SqlClient.SqlException sqlEx
					&& sqlEx.Number == 547)
					errors.Add(($"Cannot delete Lyric Segment due to related data. {sqlEx.Message}"));

				errors.Add(ex.Message);
			}
			catch (Exception ex)
			{
				errors.Add(ex.Message);
			}			

			if (errors.Any() || notFoundSegments.Any())
			{
				return Ok(new
				{
					Deleted = foundSegments,
					NotDeleted = notFoundSegments
				});
			}

			return NoContent();
		}

	}
}
