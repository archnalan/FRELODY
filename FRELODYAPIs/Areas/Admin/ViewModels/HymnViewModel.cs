using FRELODYAPP.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using FRELODYAPP.Interfaces;

namespace FRELODYAPP.Areas.Admin.ViewModels
{
	public class SongViewModel
	{
		[Required]		
		public int Number { get; set; }

		[Display(Name = "SDAH-")]
		public string DisplaySong => $"SDAH-{Number}";
		[Required]
		[StringLength(100)]
		public string Title { get; set; }		

		[StringLength(100)]
		public string? WrittenDateRange { get; set; }

		[StringLength(100)]
		public string? WrittenBy { get; set; }

		[StringLength(255)]
		public string? History { get; set; }

		[StringLength(200)]
		public string AddedBy { get; set; }

		public DateTime AddedDate { get; set; }
		public int CategoryId { get; set; }

		//Category Dropdown
		public IEnumerable<SelectListItem> Categories { get; set; }		

	}
}
