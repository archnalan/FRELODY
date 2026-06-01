namespace FRELODY.Docs.Models;

/// <summary>
/// Visibility tier for a documentation page or nav item. Higher = more restrictive.
/// Mirrors FRELODY's real access model:
///   Public  — open to everyone, no sign-in required.
///   Member  — any signed-in FRELODY account (e.g. the /compose editor, playlists, songbooks).
///   Premium — a billing-active account (PremiumTrial / ActiveRecurring / ActiveLifetime):
///             unlimited song analysis, Today's Songs, extended length, chord-chart printing.
///   Admin   — SuperAdmin / Owner / Admin (organization & platform administration).
/// </summary>
public enum Audience
{
    /// <summary>Open to everyone, no sign-in required.</summary>
    Public = 0,

    /// <summary>Requires any signed-in FRELODY account.</summary>
    Member = 1,

    /// <summary>Requires a billing-active (premium) FRELODY account.</summary>
    Premium = 2,

    /// <summary>Requires an administrator role (SuperAdmin / Owner / Admin).</summary>
    Admin = 3,
}
