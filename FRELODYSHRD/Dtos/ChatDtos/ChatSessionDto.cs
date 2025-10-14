using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.ChatDtos
{
    public class ChatSessionDto : BaseEntityDto
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

        public ICollection<ChatMessageDto>? Messages { get; set; }
    }
}
