using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SongsWithChords.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using SongsWithChords.Dtos;

namespace FRELODYSHRD.Dtos
{
    public class UserFeedbackDto : BaseEntityDto
    {
        public long? FeedbackId { get; set; }

        [Required]
        [StringLength(255)]
        public string UserComment { get; set; }

        public Guid? SongId { get; set; }

        [StringLength(50)]
        public string? UserId { get; set; }

        [EnumDataType(typeof(FeedbackStatus))]
        public FeedbackStatus? Status { get; set; }

        [ForeignKey(nameof(SongId))]
        public SongDto? Song { get; set; }
    }

}
