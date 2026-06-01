# Public song and playlist pages

> Open a shared song or playlist link to a clean public page, no account needed.

## Overview

When someone shares a song or playlist with you, the link opens a public page anyone can view. You don't need a FRELODY account to look.

Shared songs use `/shared/{token}` and shared playlists use `/shared/playlist/{token}`. These addresses serve a tidy page with the right title, description and preview image, so the link looks good when it's posted or sent on.

From there, real browsers are sent on to the in-app view automatically — a song lands on `/songs/landing/{SongId}` and a playlist on `/playlists/landing/{PlaylistId}/detail`. If automatic redirects are off, there's an **Open in Frelody →** link to follow.

If a link no longer works you'll see a clear message: **This share link has expired** for an expired link, or **Shared content not found** if it can't be located.

## Steps

1. Tap a shared link, for example `/shared/{token}`.
2. The public page loads with the song or playlist's preview details.
3. Wait a moment to be sent on to the full view, or select **Open in Frelody →**.
4. To save or organise what you're viewing, create an account. See [Sign in and sign out](/docs/getting-started/sign-in-and-sign-out).

## Tips

- These pages are made to be crawler-friendly, so the link shows a proper preview on chat and social apps. See [Social cards (Open Graph)](/docs/sharing/social-cards).
- Share links can expire — if yours has, ask the sender for a fresh one.

<figure class="img-frame" style="aspect-ratio: 16 / 9;">
  <div class="img-frame-placeholder" role="img" aria-label="A public FRELODY shared song landing page">
    <span class="img-frame-caption">A public FRELODY shared song landing page</span>
  </div>
</figure>

## Related pages

- [Share links](/docs/sharing/share-links)
- [Social cards (Open Graph)](/docs/sharing/social-cards)
- [Previewing a song without an account](/docs/library/song-preview)

<!-- AI WRITER BRIEF
slug: sharing/public-landings
audience: Public
Write this page following _README.md (tone, page structure, image & video embed patterns)
and the page-by-page facts in _WRITER-BRIEF.md. Replace the Overview placeholder above and
add any task Steps / Tips / Related pages sections that apply. Keep this marker in place.
-->
