﻿using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos
{
	public class SongBookCreateDto
	{
		[Required]
		[StringLength(50)]
		public string Title { get; set; }

		[StringLength(255)]
		public string? SubTitle { get; set; }

        public string? Slug { get; set; }

        public string? Description { get; set; }

		[Required]
		[StringLength(255)]
		public string? Publisher { get; set; }

		[Required]
		[StringLength(50)]
		public string? Language { get; set; }
	}
}
