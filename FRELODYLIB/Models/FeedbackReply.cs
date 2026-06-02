using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    public class FeedbackReply : BaseEntity
    {
        [Required]
        public string FeedbackId { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public FeedbackReplyDirection Direction { get; set; }

        [StringLength(100)]
        public string? AuthorName { get; set; }

        [StringLength(50)]
        public string? AuthorUserId { get; set; }

        [ForeignKey(nameof(FeedbackId))]
        public UserFeedback? Feedback { get; set; }
    }
}
