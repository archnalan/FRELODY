using FRELODYAPP.Dtos.SubDtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongSectionsApi
    {
        [Get("/api/song-sections/get-combo-box-song-sections")]
        Task<IApiResponse<List<ComboBoxDto>>> GetComboBoxSongSections();

    }
}
