using FRELODY.Docs.Models;

namespace FRELODY.Docs.Services;

/// <summary>
/// The curated list of documentation media slots, generated from the <c>data-media-slot</c>
/// placeholders in <c>wwwroot/content/**</c>. Drives the SuperAdmin media manager page (what
/// belongs where) and is the authoritative set of keys the docs site will inject.
///
/// To regenerate after adding/removing placeholders, re-run the annotation pass (see
/// <c>media-plan.md</c> Phase 3) and rebuild this list from <c>media-registry-rows.json</c>.
/// </summary>
public static class MediaRegistry
{
    public static readonly IReadOnlyList<MediaSlot> Slots = new[]
    {
        new MediaSlot("account-library-and-recovery--1", "account/library-and-recovery", MediaKind.Image, "16 / 9", "The Recovery tab listing auto-saved editing drafts with timestamps"),
        new MediaSlot("account-password-and-security--1", "account/password-and-security", MediaKind.Image, "4 / 3", "The Change Password dialog open over the Account security section"),
        new MediaSlot("account-settings--1", "account/settings", MediaKind.Image, "16 / 9", "The Settings tab showing the Theme dropdown, Notify switch and song display Mode control"),
        new MediaSlot("account-your-profile--1", "account/your-profile", MediaKind.Image, "16 / 9", "The FRELODY profile page with the header and tabbed navigation"),
        new MediaSlot("administration-manage-members--1", "administration/manage-members", MediaKind.Image, "16 / 9", "The Members page with invite, role-edit and enable controls"),
        new MediaSlot("administration-org-dashboard--1", "administration/org-dashboard", MediaKind.Image, "16 / 9", "The organization dashboard with metric cards and links to Members, Projects and Plans"),
        new MediaSlot("administration-products-and-pricing--1", "administration/products-and-pricing", MediaKind.Image, "16 / 9", "The Plans and pricing editor listing plans with their feature toggles"),
        new MediaSlot("administration-tenants--1", "administration/tenants", MediaKind.Image, "16 / 9", "The Tenant Management page with a search box and tenant list"),
        new MediaSlot("compose-chord-charts--1", "compose/chord-charts", MediaKind.Image, "4 / 5", "The chord chart wizard on the Draw step with a diagram in progress"),
        new MediaSlot("compose-chord-draw--1", "compose/chord-draw", MediaKind.Image, "4 / 5", "The chord-draw fret diagram editor with a chord shape placed on the fretboard"),
        new MediaSlot("compose-chord-reference--1", "compose/chord-reference", MediaKind.Image, "16 / 9", "The chord reference at /chords showing a searchable grid of chord cards"),
        new MediaSlot("compose-overview--1", "compose/overview", MediaKind.Video, "16 / 9", "A tour of the Compose song editor"),
        new MediaSlot("compose-overview--2", "compose/overview", MediaKind.Image, "16 / 9", "The Compose Song Editor showing the song form and tabbed section boards"),
        new MediaSlot("compose-viewing-chord-charts--1", "compose/viewing-chord-charts", MediaKind.Image, "4 / 5", "A chord chart detail page showing the chord, fret position and diagram"),
        new MediaSlot("discover-chord-playback--1", "discover/chord-playback", MediaKind.Image, "16 / 9", "The playback view with synced chord grid, beat strip and meta chips"),
        new MediaSlot("discover-free-limits--1", "discover/free-limits", MediaKind.Image, "4 / 3", "The daily quota badge and access sheet shown on an analysis"),
        new MediaSlot("discover-overview--1", "discover/overview", MediaKind.Image, "16 / 9", "The Discover search page with the YouTube and TikTok source toggle"),
        new MediaSlot("discover-paste-a-link--1", "discover/paste-a-link", MediaKind.Video, "16 / 9", "Pasting a song link into FRELODY Discover"),
        new MediaSlot("discover-tiktok-analysis--1", "discover/tiktok-analysis", MediaKind.Image, "9 / 16", "A TikTok analysis with the vertical floating player and chord chart"),
        new MediaSlot("discover-todays-songs--1", "discover/todays-songs", MediaKind.Image, "16 / 9", "The Today's Songs grid showing unlocked songs with time remaining"),
        new MediaSlot("discover-unlimited-analysis--1", "discover/unlimited-analysis", MediaKind.Image, "16 / 9", "An analysis running without a daily-limit prompt on Premium"),
        new MediaSlot("discover-youtube-analysis--1", "discover/youtube-analysis", MediaKind.Image, "16 / 9", "A completed YouTube analysis with the synced chord playback view"),
        new MediaSlot("getting-started-create-an-account--1", "getting-started/create-an-account", MediaKind.Image, "4 / 3", "The Create your FRELODY account form with name and email fields and a step indicator"),
        new MediaSlot("getting-started-how-frelody-works--1", "getting-started/how-frelody-works", MediaKind.Video, "16 / 9", "How FRELODY turns a song link into a playable chord chart"),
        new MediaSlot("getting-started-how-frelody-works--2", "getting-started/how-frelody-works", MediaKind.Image, "16 / 9", "A synced chord grid playing alongside a song with a beat strip below"),
        new MediaSlot("getting-started-sign-in-and-sign-out--1", "getting-started/sign-in-and-sign-out", MediaKind.Image, "4 / 3", "The Welcome back sign-in card with email and password fields and a Continue with Google button"),
        new MediaSlot("getting-started-themes-and-display--1", "getting-started/themes-and-display", MediaKind.Image, "1 / 1", "The light and dark theme toggle in the FRELODY navigation bar"),
        new MediaSlot("getting-started-welcome--1", "getting-started/welcome", MediaKind.Image, "16 / 9", "The FRELODY Discover home page with a search box ready for a song link"),
        new MediaSlot("help-get-support--1", "help/get-support", MediaKind.Image, "4 / 3", "The in-app feedback dialog for sending a message to the FRELODY team"),
        new MediaSlot("library-playlists--1", "library/playlists", MediaKind.Image, "16 / 9", "The FRELODY playlists page with folders and saved playlists"),
        new MediaSlot("library-song-preview--1", "library/song-preview", MediaKind.Image, "16 / 9", "A FRELODY song preview as seen by a signed-out visitor"),
        new MediaSlot("library-songbooks--1", "library/songbooks", MediaKind.Image, "16 / 9", "The FRELODY Song Books page showing songbook cards"),
        new MediaSlot("library-songs-library--1", "library/songs-library", MediaKind.Image, "16 / 9", "The FRELODY songs library with search and a grid of saved songs"),
        new MediaSlot("library-viewing-a-song--1", "library/viewing-a-song", MediaKind.Image, "16 / 9", "A FRELODY song page showing chords lined up above the lyrics"),
        new MediaSlot("organizations-create-an-organization--1", "organizations/create-an-organization", MediaKind.Image, "4 / 3", "The Create your organization form with name, industry and country fields"),
        new MediaSlot("organizations-join-an-organization--1", "organizations/join-an-organization", MediaKind.Image, "4 / 3", "The Join an organization form with the Organization ID field"),
        new MediaSlot("organizations-members-and-roles--1", "organizations/members-and-roles", MediaKind.Image, "16 / 9", "The Members page listing organization members with their roles"),
        new MediaSlot("organizations-overview--1", "organizations/overview", MediaKind.Image, "16 / 9", "The My organization page showing organization name, member count and country"),
        new MediaSlot("organizations-projects--1", "organizations/projects", MediaKind.Image, "16 / 9", "The Projects page with Songs, Playlists and Songbooks tabs"),
        new MediaSlot("pricing-how-premium-works--1", "pricing/how-premium-works", MediaKind.Image, "16 / 9", "The Discover analysis screen with the daily-limit badge cleared after upgrading to Premium"),
        new MediaSlot("pricing-paypal-checkout--1", "pricing/paypal-checkout", MediaKind.Image, "4 / 3", "The Complete your upgrade window with the PayPal tab selected and the PayPal buttons loaded"),
        new MediaSlot("pricing-pesapal-checkout--1", "pricing/pesapal-checkout", MediaKind.Image, "4 / 3", "The Complete Your Payment window showing the PesaPal gateway for mobile money, card and bank transfer"),
        new MediaSlot("pricing-plans--1", "pricing/plans", MediaKind.Image, "16 / 9", "FRELODY pricing page showing the Starter and Creator plan cards with a monthly, yearly and lifetime billing toggle"),
        new MediaSlot("printing-download-this-guide--1", "printing/download-this-guide", MediaKind.Image, "4 / 3", "A browser print dialog set to Save as PDF showing a clean FRELODY documentation page"),
        new MediaSlot("printing-export-pdf--1", "printing/export-pdf", MediaKind.Image, "4 / 3", "A browser print dialog with the destination set to Save as PDF for a FRELODY chord sheet"),
        new MediaSlot("printing-print-chord-charts--1", "printing/print-chord-charts", MediaKind.Image, "4 / 3", "The song share dropdown open with the Print option highlighted"),
        new MediaSlot("sharing-public-landings--1", "sharing/public-landings", MediaKind.Image, "16 / 9", "A public FRELODY shared song landing page"),
        new MediaSlot("sharing-share-links--1", "sharing/share-links", MediaKind.Image, "4 / 3", "The song share menu with the Share Link option highlighted"),
        new MediaSlot("sharing-social-cards--1", "sharing/social-cards", MediaKind.Image, "16 / 9", "A FRELODY Open Graph card previewing a shared song in a chat app"),
    };

    private static readonly Dictionary<string, MediaSlot> ByKey =
        Slots.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

    public static MediaSlot? Find(string key) =>
        key is not null && ByKey.TryGetValue(key, out var s) ? s : null;
}
