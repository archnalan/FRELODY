using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.ChatDtos
{
    public class ChatMessageDto : BaseEntityDto
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
