namespace SongsWithChords.Interfaces
{
	public interface ILyricHandler
	{
		public Task<List<string>> ExtractTxtFileAsync(string filePath);
		public Task<List<string>> ExtractWordDocAsync(string filePath);
		public Task<List<string>> ExtractPdfAsync(string filePath);
	}
}
