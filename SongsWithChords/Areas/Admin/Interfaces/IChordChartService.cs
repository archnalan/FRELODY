using SongsWithChords.Dtos.WithUploads;
using SongsWithChords.Models;
using SongsWithChords.ServiceHandler;

namespace SongsWithChords.Areas.Admin.Interfaces
{
	public interface IChordChartService	
	{		
		Task<ServiceResult<List<ChartEditDto>>> GetAllChordChartsAsync();

		/// <summary>
		/// Gets the chord chart by the specified ID.
		/// </summary>
		/// <param name="id">The ID of the chord chart.</param>
		/// <returns>A service result containing the chord chart DTO or an error message.</returns>
		/// <exception cref="ApplicationException">Thrown when the chord chart with the specified ID does not exist.</exception>
		Task<ServiceResult<ChartWithUploadsDto>> GetChordChartByIdAsync(Guid id);

		//Task<(ChartWithUploadsDto, string)> GetChordChartByIdAsync(int id);
		//Task<(ChartWithParentChordDto, string)> GetChordChartWithChordByIdAsync(int id);

		/// <summary>
		/// creates a chord chart 
		/// </summary>
		/// <param name="chordChartDto"></param>
		/// <returns></returns>
		Task<ServiceResult<ChartEditDto>> CreateChordChartAsync(ChartCreateDto chordChartDto);

		/// <summary>
		/// Updates a chord chart
		/// </summary>
		/// <param name="chordChartDto"></param>
		/// <returns></returns>
		Task<ServiceResult<ChartEditDto>> EditChordChartAsync(ChartEditDto chordChartDto);

		/// <summary>
		/// deletes a chart from the database
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<ServiceResult<bool>> DeleteChordChartByIdAsync(Guid id);
	}
}
