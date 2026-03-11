using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYUI.Shared.Services;

public interface ISongExtractionAiService
{
    /// <summary>
    /// Sends the original content alongside the extracted segments to an AI model
    /// to refine chord placement, detect song sections, and fix extraction inconsistencies.
    /// </summary>
    /// <param name="originalContent">The raw text (or PDF-extracted text) that was used for extraction.</param>
    /// <param name="segments">The segments produced by ChordLyricExtrator.</param>
    /// <returns>A refined list of segments with corrected chords, section labels, and ordering.</returns>
    Task<List<SegmentCreateDto>> RefineExtractionAsync(string originalContent, ICollection<SegmentCreateDto> segments);

    /// <summary>
    /// Whether the AI refinement feature is currently available (API key configured, feature enabled, etc.).
    /// </summary>
    bool IsAvailable { get; }
}
