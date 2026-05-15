using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.ModelTypes
{
    public enum SongSection
    {
        unknown = 0,
        Intro,
        Verse,
        PreChorus,
        Chorus,
        PostChorus,
        Bridge,
        Interlude,
        Solo,
        Refrain,
        Coda,
        Outro,
        /// <summary>
        /// Plain (chord-less) lyrics block, typically the "Full Lyrics" section at the
        /// bottom of scraped chord pages (e.g. bradwarden.com hymn chords). Rendered as
        /// a collapsible section in the player so it doesn't dominate the chord view.
        /// </summary>
        FullLyrics
    }
}
