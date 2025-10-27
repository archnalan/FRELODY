using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models
{
    public class ArtistAlbumPairViewModel
    {
        public string ArtistId { get; set; } = default!;
        public string ArtistName { get; set; } = default!;
        public string? AlbumId { get; set; }
        public string? AlbumTitle { get; set; }
        public int? TrackNumber { get; set; }
    }
}
