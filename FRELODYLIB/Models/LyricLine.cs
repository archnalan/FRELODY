﻿using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class LyricLine:BaseEntity
	{
        public Guid Id { get; set; }
        public long LyricLineOrder { get; set; }
		public SongSection PartName { get; set; } 
        public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null
		public int? RepeatCount { get; set; }

        //Navigation prop for verse,chorus and bridge and chord
        public Guid? VerseId { get; set; }
		public Guid? ChorusId { get; set; }
		public Guid? BridgeId { get; set; }
		public ICollection<LyricSegment>? LyricSegments { get; set; }
	}
}
