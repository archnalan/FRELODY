using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class SongCollection : BaseEntity
    {
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
        public DateTime? CollectionDate { get; set; } = DateTime.UtcNow;

        public bool? IsPublic { get; set; } = true;

        public bool? IsFeatured { get; set; } = false;

        public int? SortOrder { get; set; }

        [StringLength(255)]
        public string? Theme { get; set; } // Optional: e.g. Gospel, Advent, Youth, etc.

        public virtual ICollection<SongBook>? SongBooks { get; set; }
    }

}
