using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class UserFeedbackCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Comment { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Title { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        public string? SongId { get; set; }

        [StringLength(50)]
        public string? UserId { get; set; }
    }
}