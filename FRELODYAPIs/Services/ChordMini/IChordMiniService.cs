using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Services.ChordMini
{
    public interface IChordMiniService
    {
        Task<YouTubeTranscriptionDto> AnalyzeAsync(YouTubeAnalyzeRequest request, CancellationToken ct = default);
    }
}
