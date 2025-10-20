using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Dtos;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos
{
    public class SongCollectionDto :BaseEntityDto
    {
        public string Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Slug { get; set; }

        [StringLength(100)]
        public string? Curator { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CollectionDate { get; set; }

        public bool? IsPublic { get; set; }

        public bool? IsFeatured { get; set; }

        public int? SortOrder { get; set; }

        [StringLength(255)]
        public string? Theme { get; set; }
        public virtual ICollection<SongBookDto>? SongBooks { get; set; }
        public virtual ICollection<SongUserCollectionDto>? SongCollections { get; set; }
    }
}