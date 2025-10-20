using FRELODYAPP.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models.PlaylistModels
{
    public class CollectionWithSongs
    {
        public SongCollectionDto Playlist { get; set; } = new();
        public List<PlaylistSongDto> Songs { get; set; } = new();
    }

}
