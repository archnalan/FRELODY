using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPP.Areas.Admin.Interfaces
{
    public interface ILyricSegment
	{
		Task<ServiceResult<List<LyricSegmentDto>>> GetAllSegmentsAsync();

		Task<ServiceResult<LyricSegmentDto>> GetSegmentByIdAsync(string id);

		Task<ServiceResult<LyricSegmentDto>> CreateSegmentAsync(LyricSegmentCreateDto segmentDto);

		Task<ServiceResult<LyricSegmentDto>> EditSegmentAsyc(string id, LyricSegmentDto segmentDto);

		Task<ServiceResult<bool>> DeleteSegmentAsync(string id);
	}
}
