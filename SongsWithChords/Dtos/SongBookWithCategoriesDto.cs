using SongsWithChords.Dtos.SubDtos;
using SongsWithChords.Models;
using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos
{
	public class SongBookWithCategoriesDto:BaseEntityDto
	{
		public Guid Id { get; set; }

		[Required]
		[StringLength(50)]
		public string Title { get; set; }

		[Required]
		[StringLength(50)]
		public string? Slug { get; set; }

		[StringLength(255)]
		public string? SubTitle { get; set; }

		public string? Description { get; set; }

		[Required]
		[StringLength(255)]
		public string? Publisher { get; set; }

		[Required]
		[DataType(DataType.Date)]
		public DateTime? PublicationDate { get; set; }

		[Required]
		[StringLength(13, MinimumLength = 10)]
		public string? ISBN { get; set; }

		[StringLength(255)]
		public string? Author { get; set; }

		[StringLength(50)]
		public string? Edition { get; set; }

		[Required]
		[StringLength(50)]
		public string? Language { get; set; }

		public ICollection<Category>? Categories { get; set; }
	}
}
