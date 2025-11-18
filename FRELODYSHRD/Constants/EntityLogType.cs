using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Constants
{
    public enum EntityLogType
    {
        Song,
        SongBook,
        Album,
        Artist,
        Playlist,
    }

    public enum ChangeLogType
    {
        Created,
        Updated,
        Deleted
    }
}
