﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FRELODYAPP.Dtos.SubDtos;

namespace FRELODYAPP.Dtos
{
    public class VerseDto: BaseEntityDto
    {
        public string? Id { get; set; }

        public string SongId { get; set; }

        [Range(0, 24)]
        public int VerseNumber { get; set; }

        [MaxLength(100)]
        public string? VerseTitle { get; set; }

        public int? RepeatCount { get; set; }

        public virtual ICollection<LyricLineDto>? LyricLines { get; set; }
    }
}
