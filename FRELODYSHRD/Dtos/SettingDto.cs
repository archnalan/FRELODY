using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Interfaces;
using FRELODYSHRD.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class SettingDto : BaseEntityDto
    {
        public string? ChordFont { get; set; }
        public string? LyricFont { get; set; }
        public string? ChordFontSize { get; set; }
        public string? LyricFontSize { get; set; }
        public SongDisplay? SongDisplay { get; set; }
        public Theme? Theme { get; set; }
        public ChordDisplay? ChordDisplay { get; set; }
        public ChordDifficulty? ChordDifficulty { get; set; }
        public PlayLevel? PlayLevel { get; set; }
        public bool? ShowNotifications { get; set; }
    }
}
