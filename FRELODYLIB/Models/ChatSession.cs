using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Models;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Models
{
    public class ChatSession : BaseEntity
    {
        public ChatStatus Status { get; set; } // Active, Closed, Archived

        [StringLength(50)]
        public string? UserId { get; set; } // Null for anonymous

        [StringLength(100)]
        public string? VisitorName { get; set; }

        [StringLength(100)]
        public string? VisitorEmail { get; set; }

        [StringLength(50)]
        public string? AssignedAdminId { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public ICollection<ChatMessage>? Messages { get; set; }
    }
}