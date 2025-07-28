using AutoMapper;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Dtos.CompositeDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYAPP.Dtos.WithUploads;
using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;
namespace FRELODYAPP.Profiles
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

            CreateMap<SongPart, SongPartDto>()
                .ForMember(dest => dest.SongId, opt => opt.MapFrom(v => v.SongId))
                .ReverseMap()
                .ForMember(dest => dest.LyricLines, opt => opt.Ignore());
            CreateMap<SongPart, VerseCreateDto>().ReverseMap();

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
