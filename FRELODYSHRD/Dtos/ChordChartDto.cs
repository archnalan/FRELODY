using Microsoft.AspNetCore.Http;
using FRELODYAPP.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FRELODYAPP.Dtos.SubDtos;

namespace FRELODYSHRD.Dtos
{
    public class ChordChartDto : BaseEntityDto
    {
        [Required]
        public string FilePath { get; set; }
        public string? ChordId { get; set; }

        [Range(1, 24)]
        public int? FretPosition { get; set; }

        [NotMapped]
        [FileExtensionValidation(new string[] { ".png, .jpg, .gif" })]
        public IFormFile? ChartUpload { get; set; }

        [StringLength(255)]
        public string? ChartAudioFilePath { get; set; }

        [StringLength(100)]
        public string? PositionDescription { get; set; }
    }
}
