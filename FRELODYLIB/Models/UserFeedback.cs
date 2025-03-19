using SongsWithChords.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SongsWithChords.Models
{
	public class UserFeedback:BaseEntity
	{
		[Key]
        public long FeedbackId { get; set; }

        [Required]
        [StringLength(255)]
        public string UserComment { get; set; }

        public Guid? SongId { get; set; }

		[StringLength(50)]
		public string? UserId { get; set; }

        [EnumDataType(typeof(FeedbackStatus))]
        public FeedbackStatus? Status { get; set; }

        [ForeignKey(nameof(SongId))]
        public Song? Song { get; set; }
    }

    public enum FeedbackStatus 
    {
        Pending,
        UnderReview,
        Addressed
    }
}
