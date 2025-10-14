using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    public class ChatMessage : BaseEntity
    {
        [Required]
        public string Message { get; set; }

        public string? ChatSessionId { get; set; }

        [StringLength(50)]
        public string? SenderId { get; set; } // UserId or AdminId

        public bool IsFromAdmin { get; set; }

        public bool IsRead { get; set; }

        public DateTimeOffset? SentAt { get; set; } = DateTimeOffset.UtcNow;
    }
}