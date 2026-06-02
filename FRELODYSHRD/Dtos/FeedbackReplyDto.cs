using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;

namespace FRELODYSHRD.Dtos
{
    public class FeedbackReplyDto : BaseEntityDto
    {
        public string? FeedbackId { get; set; }
        public string? Body { get; set; }
        public FeedbackReplyDirection Direction { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorUserId { get; set; }
    }
}
