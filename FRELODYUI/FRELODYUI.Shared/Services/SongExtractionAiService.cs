using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYUI.Shared.RefitApis;

namespace FRELODYUI.Shared.Services;

public class SongExtractionAiService : ISongExtractionAiService
{
    private readonly ISongAiApi _songAiApi;

    public SongExtractionAiService(ISongAiApi songAiApi)
    {
        _songAiApi = songAiApi;
    }

    public bool IsAvailable => true;

    public async Task<List<SegmentCreateDto>> RefineExtractionAsync(
        string originalContent, ICollection<SegmentCreateDto> segments, string? imageBase64 = null)
    {
        var request = new SongAiRefineRequest
        {
            OriginalContent = originalContent,
            Segments = segments.ToList(),
            ImageBase64 = imageBase64
        };

        var response = await _songAiApi.RefineExtraction(request);

        if (response.IsSuccessStatusCode && response.Content != null)
            return response.Content;

        return segments.ToList();
    }
}
