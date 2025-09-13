using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IChordChartsApi
    {
        [Get("/api/chord-charts/get-all-chord-charts")]
        Task<IApiResponse<List<ChordChartEditDto>>> GetAllChordCharts();

        [Get("/api/chord-charts/get-chord-chart-by-id")]
        Task<IApiResponse<ChordChartEditDto>> GetChordChartById([Query] string id);

        [Get("/api/chord-charts/get-charts-by-chord-id")]
        Task<IApiResponse<List<ChordChartEditDto>>> GetChartsByChordId([Query] string chordId);

        [Get("/api/chord-charts/get-chart-with-parent-chord-by-id")]
        Task<IApiResponse<ChartWithParentChordDto>> GetChartWithParentChordById([Query] string id);

        [Post("/api/chord-charts/create-chord-chart")]
        Task<IApiResponse<ChordChartEditDto>> CreateChordChart([Body] ChordChartCreateDto chartDto);

        [Put("/api/chord-charts/update-chord-chart")]
        Task<IApiResponse<ChordChartEditDto>> UpdateChordChart([Body] ChordChartEditDto chartDto);

        [Delete("/api/chord-charts/delete-chord-chart")]
        Task<IApiResponse<object>> DeleteChordChart([Query] string id);

        [Multipart]
        [Post("/api/chord-charts/create-chord-chart-files")]
        Task<IApiResponse<ChordChartEditDto>> CreateChordChartFiles(
           [AliasAs("chartImage")] StreamPart? chartImage,
           [AliasAs("chartAudio")] StreamPart? chartAudio,
           [Query] string chartDataJson);

        [Multipart]
        [Put("/api/chord-charts/update-chord-chart-files")]
        Task<IApiResponse<ChordChartEditDto>> UpdateChordChartFiles(
            [AliasAs("chartImage")] StreamPart? chartImage,
            [AliasAs("chartAudio")] StreamPart? chartAudio,
            [Query] string chartDataJson);
    }
}