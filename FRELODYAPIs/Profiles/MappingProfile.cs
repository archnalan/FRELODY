using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Mapster;

namespace FRELODYAPP.Profiles
{
    public static class MappingConfig
    {
        public static void RegisterMappings()
        {
            // SongPart → SongPartDto: map SongId explicitly
            // SongPartDto → SongPart: ignore LyricLines on reverse
            TypeAdapterConfig<SongPartDto, SongPart>.NewConfig()
                .Ignore(dest => dest.LyricLines);

            // ChordEditDto → Chord: ignore ChordCharts on reverse
            TypeAdapterConfig<ChordEditDto, Chord>.NewConfig()
                .Ignore(dest => dest.ChordCharts);

            // Chord → ChordWithChartsDto: map ChordCharts to Charts
            TypeAdapterConfig<Chord, ChordWithChartsDto>.NewConfig()
                .Map(dest => dest.Charts, src => src.ChordCharts);

            // Chord → ChordWithChartsCreateDto: map ChordCharts to Charts
            TypeAdapterConfig<Chord, ChordWithChartsCreateDto>.NewConfig()
                .Map(dest => dest.Charts, src => src.ChordCharts);

            // Chord → ChordWithOneChartDto: map ChordCharts to ChordChart
            TypeAdapterConfig<Chord, ChordWithOneChartDto>.NewConfig()
                .Map(dest => dest.ChordChart, src => src.ChordCharts);

            // ChordWithOneChartDto → Chord: ignore ChordCharts on reverse
            TypeAdapterConfig<ChordWithOneChartDto, Chord>.NewConfig()
                .Ignore(dest => dest.ChordCharts);
        }
    }
}
