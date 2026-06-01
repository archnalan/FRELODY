# Analyse a YouTube song

> Turn a YouTube video into a synced chord chart, then save it to your library.

## Overview

Once you've picked a YouTube song in **Discover**, FRELODY opens its analysis page at `/discover/{videoId}`. Here it works out the chords, lines them up with the audio, and gives you a chart you can play along with.

The page header shows the video thumbnail, title and channel, a quota badge tracking your free analyses for the day, a **Share** button that copies a link, and a back arrow to **Discover**. The main area moves through clear states: it starts at "Ready to analyze" with an **Analyze chords** button, shows progress while it works, then renders the full playback view when it's done.

Sometimes you'll hit a stopping point instead. If you've used your free songs for the day you'll see "Daily limit reached"; a song over the length cap shows "Too long to analyze"; and if something goes wrong you'll get "Analysis failed" with a **Try again** button. The free limits and length caps are explained in [Free daily limits and song length](/docs/discover/free-limits).

<figure class="img-frame" style="aspect-ratio: 16 / 9;">
  <div class="img-frame-placeholder" role="img" aria-label="A completed YouTube analysis with the synced chord playback view">
    <span class="img-frame-caption">A completed YouTube analysis with the synced chord playback view</span>
  </div>
</figure>

## Steps

1. Pick a YouTube song in **Discover** to open its page at `/discover/{videoId}`.
2. Select **Analyze chords**. FRELODY detects the chords and syncs them to the audio.
3. When it finishes, play along with the chord chart, beat strip and meta chips.
4. To keep it, select **Save to Library**. If you're not signed in yet, FRELODY sends you to sign in first and brings you back.
5. Use **Re-analyze** if you want to run the detection again.

## Tips

- Saving takes you to the song's page in your library, ready for next time.
- Need to sign in? See [Sign in and sign out](/docs/getting-started/sign-in-and-sign-out).
- The full playback controls, including slow-down and looping, are covered in [Chord playback and timeline](/docs/discover/chord-playback).

## Related pages

- [Chord playback and timeline](/docs/discover/chord-playback)
- [Free daily limits and song length](/docs/discover/free-limits)
- [Today's Songs](/docs/discover/todays-songs)
- [Your songs library](/docs/library/songs-library)

<!-- AI WRITER BRIEF
slug: discover/youtube-analysis
audience: Member
Write this page following _README.md (tone, page structure, image & video embed patterns)
and the page-by-page facts in _WRITER-BRIEF.md. Replace the Overview placeholder above and
add any task Steps / Tips / Related pages sections that apply. Keep this marker in place.
-->
