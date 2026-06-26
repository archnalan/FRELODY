namespace FRELODYSHRD.Constants
{
    /// <summary>
    /// Machine-readable classification of why an analysis request was turned away at
    /// the access gate. Persisted on the request log so the superadmin "Requests"
    /// review can group demand by cause and pick the right action: whitelist an
    /// over-long song, tune the daily quota, or read sign-up conversion pressure.
    /// </summary>
    public static class GateDenialReason
    {
        /// <summary>Free user; video exceeds the free cap but fits the premium cap — upgrading unblocks it.</summary>
        public const string TooLongUpgradeable = "too-long-upgradeable";

        /// <summary>Video exceeds even the premium cap — only a whitelist (or a higher global cap) unblocks it.</summary>
        public const string TooLongHard = "too-long-hard";

        /// <summary>Signed-in free user hit their daily analyzed-song quota.</summary>
        public const string LimitReached = "limit-reached";

        /// <summary>Anonymous visitor; sign-in is required before analyzing.</summary>
        public const string Unauthenticated = "unauthenticated";

        /// <summary>True when the reason is one of the duration caps (a whitelist candidate).</summary>
        public static bool IsTooLong(string? reason) =>
            reason == TooLongUpgradeable || reason == TooLongHard;
    }
}
