﻿using FRELODYAPP.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongBooksApi
    {
        [Get("/api/song-books")]
        Task<IApiResponse<List<SongBookDto>>> GetAllSongBooks();
    }
}
