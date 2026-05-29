namespace FRELODYAPIs.Options
{
    /// <summary>
    /// Tunables for the analyzed-song money loop. Bound from the "Monetization"
    /// configuration section.
    /// </summary>
    public sealed class MonetizationOptions
    {
        public const string SectionName = "Monetization";

        /// <summary>Free distinct analyzed songs a non-premium user may unlock per UTC day.</summary>
        public int FreeAnalyzedSongsPerDay { get; set; } = 2;

        /// <summary>How long an unlocked analyzed song stays available before it expires.</summary>
        public int AvailabilityWindowHours { get; set; } = 24;

        /// <summary>
        /// Max source duration (seconds) a free user may analyze. Covers ~90–95% of
        /// songs at 8 min; lower it to reduce server load. 0 disables the limit.
        /// </summary>
        public int FreeMaxDurationSeconds { get; set; } = 480;

        /// <summary>
        /// Max source duration (seconds) a premium user may analyze. 0 disables the limit.
        /// </summary>
        public int PremiumMaxDurationSeconds { get; set; } = 1200;

        /// <summary>
        /// When false, the quota is recorded and reported but never blocks playback —
        /// lets the backend ship and be verified before the paywall/sign-in UI lands.
        /// Flip to true once the Discover UI handles the 402/sign-in responses.
        /// </summary>
        public bool EnforceAnalyzedQuota { get; set; } = false;
    }
}
