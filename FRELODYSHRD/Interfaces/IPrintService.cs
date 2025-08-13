using FRELODYAPP.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Interfaces
{
    public interface IPrintService
    {
        Task PrintSongAsync(SongDto song);
        Task<bool> IsPrintAvailableAsync();
    }
}
