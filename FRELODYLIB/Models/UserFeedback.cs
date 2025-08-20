using FRELODYSHRD.ModelTypes;
using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class UserFeedback:BaseEntity
	{
        [Required]
        [StringLength(100)]
        public string Subject { get; set; }

        [Required]
        [StringLength(255)]
        public string Comment { get; set; }

        [StringLength(100)]
        public string? Title { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(100)]
        public string? FullName { get; set; }
        public string? SongId { get; set; }

		[StringLength(50)]
		public string? UserId { get; set; }

        [EnumDataType(typeof(FeedbackStatus))]
        public FeedbackStatus? Status { get; set; }

        [ForeignKey(nameof(SongId))]
        public Song? Song { get; set; }
    }
}
