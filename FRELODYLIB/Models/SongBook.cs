﻿using FRELODYAPP.Models.SubModels;
using FRELODYLIB.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class SongBook:BaseEntity
	{
		[Required]
		[StringLength(50)]
		public string Title { get; set; }

		[Required]
		[StringLength(50)]
		public string Slug { get; set; }

		[StringLength(255)]
		public string? SubTitle { get; set; }		
		
		public string? Description { get; set; }

		[StringLength(255)]
		public string? Publisher { get; set; }

		[DataType(DataType.Date)]
		public DateTime? PublicationDate { get; set; }

		[StringLength(13, MinimumLength =10)]
		public string? ISBN { get; set; }

		[StringLength (255)]
        public string? Author { get; set; }

		[StringLength(50)]
		public string? Edition { get; set; }

		[StringLength(50)]
		public string? Language { get; set; }

		public string? CollectionId { get; set; }
		
		[ForeignKey(nameof(CollectionId))]
        public virtual SongCollection? Collection { get; set; }

        public ICollection<Category>? Categories { get; set; }

    }
}
