﻿using FRELODYAPP.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos
{
	public class ChordWithChartsCreateDto
	{
		public string ChordName { get; set; }

		[Range(1, 3)]
		public ChordDifficulty? Difficulty { get; set; }		
		public string? ChordAudioFilePath { get; set; }
		public List<ChordChartCreateDto>? Charts { get; set; }
	}
}
