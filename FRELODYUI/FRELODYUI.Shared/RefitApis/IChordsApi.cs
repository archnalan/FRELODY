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
    public interface IChordsApi
    {
        [Get("/api/chords/get-all-chords")]
        Task<IApiResponse<List<ChordSimpleDto>>> GetAllChords();

        [Get("/api/chords/get-chords-with-charts")]
        Task<IApiResponse<List<ChordWithChartsDto>>> GetChordsWithCharts();

        [Get("/api/chords/get-chord-by-id")]
        Task<IApiResponse<ChordEditDto>> GetChordById([Query] string id);

        [Get("/api/chords/getchord-with-chartsbyid")]
        Task<IApiResponse<ChordWithChartsDto>> GetChordWithChartsById([Query] string id);

        [Post("/api/chords/create-chord")]
        Task<IApiResponse<ChordEditDto>> CreateChord([Body] ChordCreateDto chordDto);

        [Post("/api/chords/create-simple-chord")]
        Task<IApiResponse<ChordSimpleDto>> CreateSimpleChord([Body] ChordSimpleDto chordDto);

        [Put("/api/chords/update-chord")]
        Task<IApiResponse<ChordEditDto>> UpdateChord([Body] ChordEditDto chordDto);

        [Delete("/api/chords/delete-chord")]
        Task<IApiResponse<object>> DeleteChord([Query] string id);
    }
}