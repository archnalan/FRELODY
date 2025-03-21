using FRELODYAPP.Data;
using FRELODYAPP.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPP.Controllers
{
	public class LyricExtractionController : Controller
	{
		private readonly ILyricHandler _lyricHandler;
		private readonly TextFileValidationAttribute _validator; 					

		public LyricExtractionController(ILyricHandler lyricHandler)
        {
            _lyricHandler = lyricHandler;
			_validator= new TextFileValidationAttribute(".txt", ".doc", ".docx", ".pdf");
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
