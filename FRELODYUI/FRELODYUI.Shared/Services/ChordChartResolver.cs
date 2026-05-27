using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FRELODYSHRD.Dtos;
using FRELODYUI.Shared.RefitApis;

namespace FRELODYUI.Shared.Services
{
    /// <summary>
    /// Resolves chord <em>names</em> to <see cref="ChordDto"/>s carrying their charts, via
    /// the chord catalog API. Shared by the song player (transpose/capo) and the Discover
    /// play-along view so both resolve charts identically.
    ///
    /// A name resolves to a real catalog chord only on an exact (case-insensitive) match;
    /// otherwise a name-only placeholder is returned. This keeps the rendered card in
    /// lockstep with the displayed chord — we never substitute a near-miss chord (e.g.
    /// showing "G7" or "Ab" when the song calls for "G").
    /// </summary>
    public interface IChordChartResolver
    {
        Task<List<ChordDto>> ResolveAsync(IReadOnlyList<string> chordNames, CancellationToken ct = default);
    }

    public class ChordChartResolver : IChordChartResolver
    {
        private readonly IChordsApi _chordsApi;

        public ChordChartResolver(IChordsApi chordsApi) => _chordsApi = chordsApi;

        public async Task<List<ChordDto>> ResolveAsync(IReadOnlyList<string> chordNames, CancellationToken ct = default)
        {
            var resolved = new List<ChordDto>(chordNames.Count);
            foreach (var name in chordNames)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(name)) continue;

                ChordDto? match = null;
                try
                {
                    var resp = await _chordsApi.SearchChords(name, 0, 5);
                    if (resp.IsSuccessStatusCode)
                        match = resp.Content?.Data?
                            .FirstOrDefault(c => c.ChordName.Equals(name, StringComparison.OrdinalIgnoreCase));
                }
                catch { /* network/parse failure → fall through to placeholder */ }

                resolved.Add(match ?? new ChordDto { ChordName = name });
            }
            return resolved;
        }
    }
}
