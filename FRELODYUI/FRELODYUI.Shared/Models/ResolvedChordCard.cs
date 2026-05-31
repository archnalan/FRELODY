using System.Collections.Generic;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.EditDtos;

namespace FRELODYUI.Shared.Models
{
    /// <summary>
    /// A chord paired with the charts already fetched for it, so a carousel can render
    /// directly via <c>PreloadedCharts</c> without its own per-card API call (avoids N+1).
    /// Produced once by the play-along view from a single resolve-with-charts round trip
    /// and shared across the Grid "Chord shapes", the hover popover, and the Timeline strip.
    /// </summary>
    public sealed record ResolvedChordCard(ChordDto Chord, List<ChordChartEditDto> Charts);
}
