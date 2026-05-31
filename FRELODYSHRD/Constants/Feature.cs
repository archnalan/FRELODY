using System;

namespace FRELODYSHRD.Constants
{
    /// <summary>
    /// The single Alex-aligned feature catalog. Both the public pricing cards
    /// (<c>Pricing.razor</c>) and the admin plan editor (<c>AdminProductsPage.razor</c>)
    /// render from this enum via <see cref="FeatureExtensions"/> — so the two pages can
    /// never drift. Append-only: never reorder/remove members (values are persisted as
    /// int indices in <c>Product.Features</c>).
    /// </summary>
    public enum Feature
    {
        AutoChordDetection,   // 0
        SlowDownPractice,     // 1
        SectionLooping,       // 2
        ChordTimeline,        // 3
        PlaylistSaving,       // 4
        SongSharing,          // 5
        PdfExport,            // 6
        UnlimitedAnalyses,    // 7
        ExtendedSongLength,   // 8
        PrioritySupport,      // 9
        SharedTeamLibrary     // 10
    }

    public static class FeatureExtensions
    {
        /// <summary>
        /// Short, neutral label for management surfaces (the /admin/products editor &amp; table).
        /// </summary>
        public static string ToFriendlyString(this Feature feature) => feature switch
        {
            Feature.AutoChordDetection => "Auto chord detection",
            Feature.SlowDownPractice   => "Slow-down practice",
            Feature.SectionLooping     => "Section looping",
            Feature.ChordTimeline      => "Synced chord timeline",
            Feature.PlaylistSaving     => "Save to playlists",
            Feature.SongSharing        => "Song sharing",
            Feature.PdfExport          => "PDF export",
            Feature.UnlimitedAnalyses  => "Unlimited analyses",
            Feature.ExtendedSongLength => "Longer songs (20 min)",
            Feature.PrioritySupport    => "Priority support",
            Feature.SharedTeamLibrary  => "Shared team library",
            _ => feature.ToString()
        };

        /// <summary>
        /// Curated, benefit-led copy for the customer-facing pricing cards — written to
        /// keep Alex invested, not to describe the system. Keep it concrete and outcome-first.
        /// </summary>
        public static string ToSalesCopy(this Feature feature) => feature switch
        {
            Feature.AutoChordDetection => "Instant chords from any YouTube or TikTok link",
            Feature.SlowDownPractice   => "Slow it down — pitch stays perfect",
            Feature.SectionLooping     => "Loop the tricky bar until it sticks",
            Feature.ChordTimeline      => "Chords that scroll in time with the song",
            Feature.PlaylistSaving     => "Save songs into practice playlists",
            Feature.SongSharing        => "Share any song with a single link",
            Feature.PdfExport          => "Export clean chord sheets as PDF",
            Feature.UnlimitedAnalyses  => "Unlimited song analyses — no daily cap",
            Feature.ExtendedSongLength => "Full-length songs, up to 20 minutes",
            Feature.PrioritySupport    => "Priority support when you need it",
            Feature.SharedTeamLibrary  => "A shared library for your whole team",
            _ => feature.ToString()
        };
    }
}
