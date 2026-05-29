using FRELODYSHRD.Dtos;

namespace FRELODYUI.Shared.Pages.Discover;

// Client-side pre-gate: decides whether a video is too long to analyze for the
// current user, BEFORE prompting sign-in or running analysis. The server applies
// the same caps authoritatively.
internal static class AnalyzedDurationGate
{
    public readonly record struct Decision(bool Blocked, bool Upgradeable, string Message);

    public static Decision Evaluate(AnalyzedLimitsDto? limits, bool isPremium, int durationSeconds)
    {
        if (limits is null || durationSeconds <= 0)
            return new Decision(false, false, string.Empty);

        var free = limits.FreeMaxDurationSeconds;
        var premium = limits.PremiumMaxDurationSeconds;
        var len = FormatMinutes(durationSeconds);

        // Too long for even premium → hard stop, no upsell.
        if (premium > 0 && durationSeconds > premium)
            return new Decision(true, false,
                $"This is {len} long. We can analyze tracks up to {FormatMinutes(premium)}.");

        var maxForUser = isPremium ? premium : free;

        // Free user, within premium's reach → upsell.
        if (!isPremium && free > 0 && durationSeconds > free)
            return new Decision(true, true,
                $"This is {len} long. Free analyzes up to {FormatMinutes(free)} — upgrade to analyze up to {FormatMinutes(premium)}.");

        return new Decision(false, false, string.Empty);
    }

    private static string FormatMinutes(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
            : ts.Seconds > 0 && ts.Minutes == 0
                ? $"{ts.Seconds}s"
                : $"{ts.Minutes} min";
    }
}
