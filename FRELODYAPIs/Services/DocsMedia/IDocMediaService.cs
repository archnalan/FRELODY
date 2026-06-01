using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.DocsDtos;

namespace FRELODYAPIs.Services.DocsMedia
{
    /// <summary>
    /// Reads and writes the documentation media manifest + image files stored on the
    /// persistent <c>frelody_media</c> volume (<c>/app/media/docs-media</c>). The manifest is a
    /// single JSON file; images are served statically at <c>/docs-media/&lt;slot&gt;.&lt;ext&gt;</c>.
    /// </summary>
    public interface IDocMediaService
    {
        /// <summary>The whole published map. Safe to call anonymously.</summary>
        Task<ServiceResult<DocMediaManifestDto>> GetManifestAsync(CancellationToken ct = default);

        /// <summary>Persist an uploaded image for a slot and return the updated entry.</summary>
        Task<ServiceResult<DocMediaEntryDto>> SaveImageAsync(string slot, IFormFile file, CancellationToken ct = default);

        /// <summary>Set a slot's YouTube video (id extracted from a URL or id) and caption.</summary>
        Task<ServiceResult<DocMediaEntryDto>> SetTextAsync(string slot, DocMediaTextUpdateDto dto, CancellationToken ct = default);

        /// <summary>Clear a slot's image (<c>kind=image</c>) or video (<c>kind=video</c>).</summary>
        Task<ServiceResult<DocMediaEntryDto>> ClearAsync(string slot, string kind, CancellationToken ct = default);
    }
}
