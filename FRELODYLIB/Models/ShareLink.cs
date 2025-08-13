using FRELODYAPP.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class ShareLink
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string SongId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string ShareToken { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(SongId))]
        public virtual Song? Song { get; set; }
    }
}
