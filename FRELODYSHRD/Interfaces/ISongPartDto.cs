using FRELODYSHRD.Dtos.CreateDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Interfaces
{
    /// <summary>
    /// Common interface for all song part DTOs (Verses, Bridges, Choruses)
    /// </summary>
    public interface ISongPartDto
    {
        ICollection<LineCreateDto> LyricLines { get; set; }
        int GetPartNumber();
    }
}
