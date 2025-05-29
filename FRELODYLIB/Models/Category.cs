using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class Category:BaseEntity
	{
		[Required]
		[StringLength(100), MinLength(1,ErrorMessage ="Category name is required.")]
		public string Name { get; set; }

		public string? ParentCategoryId { get; set; }//Nullable Main Category

		public int? Sorting {  get; set; } //CategoryOrder

		[StringLength(255)]
		public string? CategorySlug { get; set; }

		public string? SongBookId { get; set; }

		[ForeignKey(nameof(ParentCategoryId))]
		public virtual Category? ParentCategory { get; set; }

		public ICollection<Category>? SubCategories { get; set; }

        public virtual ICollection<Song>? Songs { get; set; }
        
    }
}
