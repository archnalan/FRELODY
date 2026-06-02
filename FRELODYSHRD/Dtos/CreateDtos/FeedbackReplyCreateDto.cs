using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class FeedbackReplyCreateDto
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }
}
