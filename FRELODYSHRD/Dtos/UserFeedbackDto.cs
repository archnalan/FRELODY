﻿using System;
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
        [StringLength(255)]
        public string Comment { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }
        public string? SongId { get; set; }

        [StringLength(50)]
        public string? UserId { get; set; }

        [EnumDataType(typeof(FeedbackStatus))]
        public FeedbackStatus? Status { get; set; }

        [ForeignKey(nameof(SongId))]
        public SongDto? Song { get; set; }
    }

}
