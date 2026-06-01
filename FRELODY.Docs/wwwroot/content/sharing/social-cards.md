# Social cards (Open Graph)

> Understand the preview card FRELODY builds for every share link so your songs look great when posted.

## Overview

When you paste a FRELODY share link into a chat or social app, it shows a rich preview rather than a bare URL. That preview is the social card, built using Open Graph and Twitter meta tags.

Every share link gets its own rendered image card, sized 1200×630, which is used as the `og:image` and `twitter:image`. The card carries the title and description from the link's snapshot.

The meta also marks the content type — `music.song` for a song or `music.playlist` for a playlist — and sets the site name (Frelody by default). When an image is present, the link uses the large summary card style.

Because the card is baked into the page's HTML, it shows correctly on WhatsApp, iMessage, Facebook, Twitter, LinkedIn, Slack and Discord without any scripts needing to run.

## How it works

1. You create a share link for a song or playlist. See [Share links](/docs/sharing/share-links).
2. FRELODY renders a 1200×630 preview image and attaches the title and description to the link.
3. You paste the link into an app that reads Open Graph tags.
4. The app shows the card — image, title and description — instead of a plain URL.

## Tips

- The card pulls from the share link's saved title, description and image, so a clear song title makes for a clearer card.
- Nothing extra is needed to make cards work — they're generated for you with every share link.

<figure class="img-frame" style="aspect-ratio: 16 / 9;">
  <div class="img-frame-placeholder" role="img" aria-label="A FRELODY Open Graph card previewing a shared song in a chat app">
    <span class="img-frame-caption">A FRELODY Open Graph card previewing a shared song in a chat app</span>
  </div>
</figure>

## Related pages

- [Share links](/docs/sharing/share-links)
- [Public song and playlist pages](/docs/sharing/public-landings)

<!-- AI WRITER BRIEF
slug: sharing/social-cards
audience: Public
Write this page following _README.md (tone, page structure, image & video embed patterns)
and the page-by-page facts in _WRITER-BRIEF.md. Replace the Overview placeholder above and
add any task Steps / Tips / Related pages sections that apply. Keep this marker in place.
-->
