using FRELODYAPIs.Services.WebSong;
using FRELODYAPP.Data;
using FRELODYAPP.Interfaces;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPP.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class LyricExtractionController : Controller
	{
		private readonly ILyricHandler _lyricHandler;
		private readonly IWebSongExtractionService _webSongExtraction;
		private readonly TextFileValidationAttribute _validator;

		public LyricExtractionController(ILyricHandler lyricHandler, IWebSongExtractionService webSongExtraction)
        {
            _lyricHandler = lyricHandler;
			_webSongExtraction = webSongExtraction;
			_validator= new TextFileValidationAttribute(".txt", ".doc", ".docx", ".pdf");
		}

		/// <summary>
		/// Server-side fetch of a chord/song page. Returns the normalized monospace
		/// pre-block text plus title/song-number metadata, ready to feed into the
		/// client's column-aligned chord/lyric parser.
		/// </summary>
		[HttpPost("from-url")]
		public async Task<ActionResult<WebSongFetchResult>> FetchFromUrl(
			[FromBody] WebSongFetchRequest request,
			CancellationToken ct)
		{
			if (request is null || string.IsNullOrWhiteSpace(request.Url))
				return BadRequest(new { message = "URL is required." });

			var result = await _webSongExtraction.FetchAsync(request.Url, ct);
			return Ok(result);
		}
        public async Task<List<string>> GetLyricsAsync(string filePath)
		{
			if (filePath == null) 
				throw new InvalidOperationException("Valid file name is required.");

			if ((_validator.IsValidFileType(filePath)) == false)
				throw new InvalidOperationException("Invalid File Type.");

			string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

			switch (fileExtension)
			{
				case ".txt":
					return await _lyricHandler.ExtractTxtFileAsync(filePath);

				case ".doc":
				case ".docx":
					return await _lyricHandler.ExtractWordDocAsync(filePath);

				case ".pdf":
					return await _lyricHandler.ExtractPdfAsync(filePath);

				default:
					throw new NotSupportedException("Unsupported file Type");

			}
	    }
	}
}
