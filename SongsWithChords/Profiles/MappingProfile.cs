using AutoMapper;
using SongsWithChords.Dtos;
using SongsWithChords.Dtos.AuthDtos;
using SongsWithChords.Dtos.CompositeDtos;
using SongsWithChords.Dtos.UserDtos;
using SongsWithChords.Dtos.WithUploads;
using SongsWithChords.Models;
using SongsWithChords.Models.SubModels;
namespace SongsWithChords.Profiles
{
	public class MappingProfile:Profile
	{
        public MappingProfile()
        {
            CreateMap<Page, PageDto>().ReverseMap();

            CreateMap<SongBook, SongBookDto>().ReverseMap();
            CreateMap<SongBook, SongBookCreateDto>().ReverseMap();
            CreateMap<SongBook, SongBookWithCategoriesDto>().ReverseMap();

            CreateMap<Category, CategoryDto>().ReverseMap();
 
            CreateMap<Song, SongCreateDto>().ReverseMap();

            CreateMap<Verse, VerseDto>()
                .ForMember(dest => dest.SongId, opt => opt.MapFrom(v => v.SongId))
                .ReverseMap()
                .ForMember(dest => dest.LyricLines, opt => opt.Ignore());
            CreateMap<Verse, VerseCreateDto>().ReverseMap();

            CreateMap<LyricLine, LyricLineDto>().ReverseMap();
            CreateMap<LyricLine, LyricLineCreateDto>().ReverseMap();
            CreateMap<LyricLine, LineVerseCreateDto>().ReverseMap();

            CreateMap<LyricSegment, LyricSegmentDto>().ReverseMap();
            CreateMap<LyricSegment, LyricSegmentCreateDto>().ReverseMap();
            
            CreateMap<Chord, ChordEditDto>()                
                .ReverseMap()
                .ForMember(dest => dest.ChordCharts, opt=>opt.Ignore());

			CreateMap<Chord, ChordCreateDto>().ReverseMap();

			CreateMap<Chord, ChordWithChartsDto>()
                .ForMember(dest=>dest.Charts, opt=>opt.MapFrom(src=>src.ChordCharts))
                .ReverseMap();
            CreateMap<Chord, ChordWithChartsCreateDto>()
                .ForMember(dest=>dest.Charts, opt=>opt.MapFrom(src=>src.ChordCharts))
				.ReverseMap();
            CreateMap<Chord, ChordWithOneChartDto>()
                .ForMember(dest=>dest.ChordChart, opt=>opt.MapFrom(src=>src.ChordCharts))                
				.ReverseMap()
                .ForMember(dest=>dest.ChordCharts, opt=>opt.Ignore());//necessary for chart from Chord

            CreateMap<ChordChart, ChordChartCreateDto>().ReverseMap();
            CreateMap<ChordChart, ChordChartEditDto>().ReverseMap();
            

            //WithUploads
            CreateMap<ChordChart, ChartCreateDto>().ReverseMap();
            CreateMap<ChordChart, ChartEditDto>().ReverseMap();
            CreateMap<ChordChart, ChartWithUploadsDto>().ReverseMap();
            
            ///////////////////////////////////////////////////////////////////////////////////////////

            CreateMap<User, CreateUserResponseDto>().ReverseMap();
            CreateMap<User, LoginResponseDto>().ReverseMap();
        }
    }
}
