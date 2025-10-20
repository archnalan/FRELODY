using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models.PlaylistModels
{

    public class MovingContext
    {
        public string SourceCollectionId { get; set; } = string.Empty;
        public string SongId { get; set; } = string.Empty;
        public string SongTitle { get; set; } = string.Empty;
    }
}
