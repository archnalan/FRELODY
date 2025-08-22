using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using FRELODYAPP.Dtos;

namespace FRELODYSHRD.Dtos
{
    public class UserFeedbackDto : BaseEntityDto
    {
        [Required]
        [StringLength(100)]
        public string Subject { get; set; }

        [Required]
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
        public string? SubmitButtonText { get; set; } = "Submit Feedback";
        public string? CommentHelperText { get; set; } = "(Optional): Provide additional details about your feedback";
        public string? CommentPlaceholder { get; set; } = "Enter your feedback...";
        public string CommentLabel { get; set; } = "Comment";
        public string SubjectPlaceholder { get; set; } = "Enter title...";
        public string SubjectLabel { get; set; } = "Subject";

        [EnumDataType(typeof(FeedbackStatus))]
        public FeedbackStatus? Status { get; set; }

        [ForeignKey(nameof(SongId))]
        public SongDto? Song { get; set; }
    }

}
