namespace FRELODYUI.Shared.Pages.Discover;

/// <summary>
/// Estimates how long each analysis stage (extract → beats → chords) will take so the
/// staged progress UI can advance on a realistic, duration-scaled schedule instead of a
/// blind fixed-interval timer that races to "Finalizing…" while the pipeline is still
/// running. Fit to measured end-to-end runs on the CPU-only box (2026-06-22):
///   YouTube Adele (~285s): beats 112s, chords 54s.  Ed Sheeran "Sapphire" (~180s): 79s / 31s.
///   TikTok short clip (~15s): beats 18s, chords 8s.
/// Beat-Transformer carries a large fixed cost (cold-load + Demucs source separation), so
/// the model is a fixed floor plus a per-second slope — not linear from zero.
/// </summary>
internal static class AnalysisStageEstimator
{
    /// <summary>Stage order matches <c>AnalysisProgress</c>: extract, beats, chords, finalize.</summary>
    public const int StageCount = 4;

    // Conservative fits. Overshooting is safe: the indeterminate bar keeps animating and the
    // host shows a "taking a little longer" subtitle, so the only thing we must avoid is
    // *under*-estimating (which is what caused the premature "Finalizing…" stall).
    private const double ExtractSeconds = 8.0;
    private const double BeatsSlope = 0.35, BeatsFloor = 13.0;
    private const double ChordsSlope = 0.17, ChordsFloor = 5.0;

    // Fallback song length when the source didn't report a duration (keeps the schedule sane).
    private const int DefaultDurationSeconds = 180;

    /// <summary>
    /// Returns the estimated seconds for the three working stages
    /// [extract, beats, chords]. The 4th stage ("Finalizing…") has no estimate — the host
    /// holds on it until the real result arrives.
    /// </summary>
    public static double[] Estimate(int? durationSeconds)
    {
        var d = durationSeconds is > 0 ? durationSeconds.Value : DefaultDurationSeconds;
        return
        [
            ExtractSeconds,
            BeatsFloor + BeatsSlope * d,
            ChordsFloor + ChordsSlope * d,
        ];
    }
}
