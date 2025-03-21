using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos
{
	public class CategoryDto
	{		
		public Guid Id { get; set; }
		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		public Guid? ParentCategoryId { get; set; }//Nullable Main Category

		public int? Sorting { get; set; } //CategoryOrder

		[StringLength(255)]
		public string? CategorySlug { get; set; }
	}
}
