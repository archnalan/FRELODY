using SongsWithChords.Dtos;
using SongsWithChords.ServiceHandler;

namespace SongsWithChords.Areas.Admin.Interfaces
{
	public interface ILyricSegment
	{
		Task<ServiceResult<List<LyricSegmentDto>>> GetAllSegmentsAsync();

		Task<ServiceResult<LyricSegmentDto>> GetSegmentByIdAsync(Guid id);

		Task<ServiceResult<LyricSegmentDto>> CreateSegmentAsync(LyricSegmentCreateDto segmentDto);

		Task<ServiceResult<LyricSegmentDto>> EditSegmentAsyc(Guid id, LyricSegmentDto segmentDto);

		Task<ServiceResult<bool>> DeleteSegmentAsync(Guid id);
	}
}
