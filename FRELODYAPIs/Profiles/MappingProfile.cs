using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
using FRELODYSHRD.Models.ChordDraw;
using Mapster;
using System.Text.Json;

namespace FRELODYAPP.Profiles
{
    public static class MappingConfig
    {
        private static readonly JsonSerializerOptions ChordJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public static ChordDrawData? DeserializeChordData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<ChordDrawData>(json, ChordJsonOptions); }
            catch { return null; }
        }

        public static string? SerializeChordData(ChordDrawData? data)
            => data == null ? null : JsonSerializer.Serialize(data, ChordJsonOptions);

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

            // ChordChart ↔ DTOs: serialize/deserialize ChordData
            TypeAdapterConfig<ChordChart, ChordChartEditDto>.NewConfig()
                .Map(dest => dest.ChordData, src => DeserializeChordData(src.ChordDataJson));

            TypeAdapterConfig<ChordChart, ChordChartDto>.NewConfig()
                .Map(dest => dest.ChordData, src => DeserializeChordData(src.ChordDataJson));

            TypeAdapterConfig<ChordChart, ChartWithParentChordDto>.NewConfig()
                .Map(dest => dest.ChordData, src => DeserializeChordData(src.ChordDataJson));

            TypeAdapterConfig<ChordChartCreateDto, ChordChart>.NewConfig()
                .Map(dest => dest.ChordDataJson, src => SerializeChordData(src.ChordData))
                .Ignore(dest => dest.RenderedSvg!)
                .Ignore(dest => dest.RenderedPngPath!);

            TypeAdapterConfig<ChordChartEditDto, ChordChart>.NewConfig()
                .Map(dest => dest.ChordDataJson, src => SerializeChordData(src.ChordData));
        }
    }
}
