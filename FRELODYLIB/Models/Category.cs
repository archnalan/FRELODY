using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class Category:BaseEntity
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		[StringLength(100), MinLength(1,ErrorMessage ="Category name is required.")]
		public string Name { get; set; }

		public Guid? ParentCategoryId { get; set; }//Nullable Main Category

		public int? Sorting {  get; set; } //CategoryOrder

		[StringLength(255)]
		public string? CategorySlug { get; set; }

		public Guid? SongBookId { get; set; }

		[ForeignKey(nameof(ParentCategoryId))]
		public virtual Category? ParentCategory { get; set; }

		[ForeignKey(nameof(SongBookId))]
		public virtual SongBook? SongBook { get; set; }

		public ICollection<Category>? SubCategories { get; set; }
        public virtual ICollection<Song>? Songs { get; set; }
        
    }
}
