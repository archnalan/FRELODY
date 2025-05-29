using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Dtos.SubDtos;

namespace FRELODYAPP.Dtos
{
	public class PageDto : BaseEntityDto
	{
		public string Title { get; set; }		
		public string? Slug { get; set; }		
		public string? Content { get; set; }
		public int? Sorting { get; set; }
	}
}
