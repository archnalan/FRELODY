using Refit;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongsApi
    {
        [Get("/api/Songs/GetSongs")]
        Task<IEnumerable<ComboBoxDto>> GetSongs();

        [Get("/api/Songs/GetSongWithChordsById/{id}")]
        Task<IEnumerable<SongDto>> GetSongWithChordsById(Guid id);

        [Get("/api/Songs/GetSongDetailsById/{id}")]
        Task<IEnumerable<SongDto>> GetSongDetailsById(Guid id);
    }
}
