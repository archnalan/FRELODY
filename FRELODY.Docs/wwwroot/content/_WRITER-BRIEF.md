# FRELODY Documentation — Writer Brief

**What this file is.** An authoritative, page-by-page input for writing each FRELODY documentation page. Every entry is grounded in the real product code (primarily `FRELODYUI/FRELODYUI.Shared` Razor components, plus `FRELODYAPIs` controllers/services and `FRELODYSHRD` constants/DTOs) as of **2026-06-01**. Use only the behaviours, routes, on-screen strings and component names recorded here; do **not** invent product behaviour. Where a slug has no backing code, it says so explicitly.

**Out of scope.** The demo/template pages `/counter` (`Counter.razor`) and `/weather` (`Weather.razor`) are leftover Blazor scaffolding and unrelated to FRELODY — never document them. There is no "POS" feature in this codebase.

**Cross-cutting facts (true across many pages):**
- Routing/layout: public marketing routes use `LandingLayout`; the app shell uses `MainLayout`/`UserLayout`; org admin uses `AdminLayout`. The site root `/` renders `Discover` in landing mode (`DiscoverLanding.razor`).
- Theme is driven by `data-bs-theme="light|dark"` on `<html>`, toggled in `NavMenu.razor` (`ToggleTheme`/`ApplyTheme`) and persisted to local storage key `"Theme"`. A per-account `Theme` setting also exists in `SettingsSection.razor`.
- Free-tier limits come from `MonetizationOptions` via `AnalyzedAccessService` (`FRELODYAPIs/Areas/Admin/LogicData/AnalyzedAccessService.cs`): **2 analyses/day** (`FreeAnalyzedSongsPerDay`), **free max 8 min / premium max 20 min** duration caps, and a **24-hour availability window** for re-play. The pricing page hard-codes the "2 / 8 min / 20 min / 24 hours" copy to match.
- Premium = `BillingStatus` in {`PremiumTrial`, `ActiveRecurring`, `ActiveLifetime`} and not expired; SuperAdmins are always premium (`AnalyzedAccessService.IsPremiumAsync`).
- Roles: platform roles `SuperAdmin/Support/User`; org roles `Owner>Admin>Manager>Editor>Contributor>Viewer>Guest` (`FRELODYSHRD/Constants/UserRoles.cs`). `Moderator` is deprecated/folded into `Admin`.
- Feature catalog (`FRELODYSHRD/Constants/Feature.cs`) drives both the pricing cards and the admin plan editor: AutoChordDetection, SlowDownPractice, SectionLooping, ChordTimeline, PlaylistSaving, SongSharing, PdfExport, UnlimitedAnalyses, ExtendedSongLength, PrioritySupport, SharedTeamLibrary.

---

## Getting started

### getting-started/welcome
**Page title:** Welcome to FRELODY
**Audience:** Public
**Authoritative sources:** `Pages/Discover/DiscoverLanding.razor` (`@page "/"`, `@page "/discover/landing"`); `Pages/Pricing.razor` hero ("Hear it. Slow it down. Play it."); `Pages/Login.razor`/`Register.razor` tagline "Your music, beautifully organized".
**Observed behaviours:**
- The product's core promise (pricing + landing copy): paste any YouTube or TikTok link → FRELODY detects chords → slow the tempo and loop tricky parts → play along. "Start free, upgrade when you're hooked."
- The home page (`/`) is the Discover search experience, not a separate splash.
**Related models / services:** none specific.
**Cross-page links:** getting-started/how-frelody-works, discover/overview, pricing/plans.

### getting-started/how-frelody-works
**Page title:** How FRELODY works
**Audience:** Public
**Authoritative sources:** `Pages/Pricing.razor` "From link to learning in three steps" section (lines ~171-196); `Pages/Discover/Discover.razor`; `Pages/Discover/DiscoverVideoContent.razor`; `Pages/Discover/YoutubePlaybackView.razor`.
**Observed behaviours:**
- Three steps from the pricing page: (1) "Paste a link" — drop in any YouTube or TikTok song; (2) "Get the chords" — FRELODY detects chords and lines them up with the audio; (3) "Slow it down & loop" — drop tempo, loop the hard bar, play along.
- Playback view shows a synced chord grid, beat strip, key/BPM/time-signature chips, capo stepper, and per-song chord-shape diagrams.
**Related models / services:** `IYouTubeApi`, `YouTubeTranscriptionDto` (SyncedChords, Beats, Bpm, KeySignature, TimeSignature).
**Cross-page links:** discover/overview, discover/chord-playback, compose/overview.

### getting-started/how-to-use-this-guide
**Page title:** How to use this guide
**Audience:** Public
**Authoritative sources:** No direct source found — write from the title and section context only. (This is a docs-site meta page about the documentation itself; describe navigation across the Getting started / Discover / Compose / Library / Pricing / Printing / Sharing / Account / Organizations / Administration / Help sections.)
**Observed behaviours:** none in product code.
**Cross-page links:** getting-started/welcome, help/faq, help/glossary.

### getting-started/create-an-account
**Page title:** Create an account
**Audience:** Public
**Authoritative sources:** `Pages/Register.razor` (`@page "/register"`, `LandingLayout`); `Pages/Registration/EmailVerificationModal.razor`; `Pages/Login.razor` (embeds `Register` when `showSignUp`).
**Observed behaviours:**
- Multi-step wizard with a step indicator (`reg-steps`, `stepLabels`, `currentStep`/`totalSteps`). Step 1 ("Create your FRELODY account") collects full name + email; supports a "personal mode" (`isPersonalMode`).
- Branding: logo `fet_melody_logo.png`, app name "FRELODY", tagline "Create your account".
- Email OTP verification via `EmailVerificationModal`.
- Registration can be invoked inline (e.g. from the Pricing login modal) and raises `OnRegisterSuccess`; supports `ReturnUrl`.
**Related models / services:** `registerModel` (UserFullName, Email, …); auth/users APIs.
**Cross-page links:** getting-started/sign-in-and-sign-out, account/your-profile.

### getting-started/sign-in-and-sign-out
**Page title:** Sign in and sign out
**Audience:** Public
**Authoritative sources:** `Pages/Login.razor` (`@page "/login"`); `Layout/ContinueAsPrompt.razor`; `Pages/Auth/MustChangePassword.razor` (`@page "/account/change-password-required"`).
**Observed behaviours:**
- Login card heading "Welcome back"; fields "Email or Username" and a conditional "Phone Number" (`ShowPhoneForm`). Honors `?returnUrl=`. Has a sign-up toggle (`showSignUp`) that swaps in the Register component.
- Google OAuth + One Tap: `ContinueAsPrompt` shows a dismissible "Continue as <name>" chip for returning/signed-out visitors; silently revives the session via refresh token, falling back to Google One Tap. Remembers dismissal for the page load.
- Forced password change route exists (`/account/change-password-required`).
**Related models / services:** `loginModel` (Email, PhoneNumber); JWT access (7-day) + refresh (30-day) tokens; `AuthHeaderHandler`.
**Cross-page links:** getting-started/create-an-account, account/password-and-security.

### getting-started/themes-and-display
**Page title:** Light and dark themes
**Audience:** Public
**Authoritative sources:** `Layout/NavMenu.razor` (`ToggleTheme`, `ApplyTheme`, `isDarkTheme`, storage key "Theme"); `Pages/useraccount/Components/SettingsSection.razor` (Appearance > Theme `<select>` over `Theme` enum; Display > Mode segmented control over `SongDisplay`).
**Observed behaviours:**
- Theme toggle in the nav sets `document.documentElement.setAttribute('data-bs-theme', 'light'|'dark')` and persists to local storage "Theme".
- Account Settings exposes a Theme dropdown (enum `Theme`), a "Notify" switch, and a song Display "Mode" segmented control: Full (`LyricsAndChordsAndCharts`), and lyrics-and-chords / lyrics-only variants (`SongDisplay`).
**Related models / services:** `Theme` enum, `SongDisplay` enum, user settings DTO.
**Cross-page links:** account/settings, library/viewing-a-song.

---

## Discover

### discover/overview
**Page title:** What Discover does
**Audience:** Public
**Authoritative sources:** `Pages/Discover/Discover.razor` (`@page "/discover"`); `Pages/Discover/DiscoverLanding.razor` (`/`).
**Observed behaviours:**
- Source toggle (tablist) between **YouTube** and **TikTok** (`SearchMode`); choice persists to local storage key `discover_source_mode`.
- YouTube headline "Find songs. Start playing."; TikTok headline "Hear a sound. Learn the chords."
- Search box: YouTube placeholder "Song title, artist, or YouTube URL…"; TikTok placeholder "Paste a TikTok video link…". Suggestion chips (YouTube only): worship songs, fingerstyle guitar, Ed Sheeran, jazz standards, hillsong.
- TikTok has no public search — paste a `tiktok.com/@user/video/…` link. Pasting a TikTok URL in either mode routes to the TikTok lane.
- Results render via `YoutubeResultGrid`; empty state "No results found"; while idle/no-search a `ChordMarquee` scrolls.
- Selecting a result navigates to `/discover/{videoId}` (app) or `/play/{videoId}` (landing).
**Related models / services:** `IYouTubeApi.Search(query, limit)`, `YouTubeVideoDto`.
**Cross-page links:** discover/paste-a-link, discover/youtube-analysis, discover/tiktok-analysis.

### discover/paste-a-link
**Page title:** Paste a song link
**Audience:** Public
**Authoritative sources:** `Pages/Discover/Discover.razor` `HandleSearch()` (lines ~205-228).
**Observed behaviours:**
- One search field accepts a song title/artist (YouTube search) OR a pasted URL. A pasted `tiktok.com` link is detected and handed to the TikTok lane regardless of toggle.
- Invalid TikTok input shows "Please paste a valid TikTok video link." YouTube search failures show "Search failed. Please try again." with a Retry button.
- Submit is disabled when the query is empty; Enter also submits.
**Related models / services:** `IYouTubeApi`.
**Cross-page links:** discover/youtube-analysis, discover/tiktok-analysis, discover/overview.

### discover/youtube-analysis
**Page title:** Analyse a YouTube song
**Audience:** Member
**Authoritative sources:** `Pages/Discover/DiscoverVideo.razor` (`@page "/discover/{VideoId}"`, BackUrl `/discover`); `Pages/Discover/DiscoverVideoContent.razor`; `Pages/Discover/YoutubePlaybackView.razor`; `Pages/Discover/AnalysisProgress.razor`; `Pages/Discover/AnalyzedQuotaBadge.razor`.
**Observed behaviours:**
- States (`AnalysisState`): Idle ("Ready to analyze" → "Analyze chords" button), in-progress, Complete (renders `YoutubePlaybackView`), Error ("Analysis failed" + "Try again"), Paywall ("Daily limit reached" / "Locked"), TooLong ("Too long to analyze").
- Header shows the video thumbnail/title/channel, a `AnalyzedQuotaBadge`, a "Share" (copy link) button, and a back arrow.
- Complete view allows "Save to Library" (signed-in only; otherwise routes to `/login?returnUrl=…`) and "Re-analyze". Saving calls `IYouTubeApi.SaveToLibrary(YouTubeSaveRequest)` then navigates to `/songs/{slug-or-id}`.
**Related models / services:** `IYouTubeApi`, `IAnalyzedAccessApi`, `YouTubeTranscriptionDto`, `YouTubeSaveRequest`, `AnalyzedAccessResultDto`.
**Cross-page links:** discover/chord-playback, discover/free-limits, discover/todays-songs, library/songs-library.

### discover/tiktok-analysis
**Page title:** Analyse a TikTok video
**Audience:** Member
**Authoritative sources:** `Pages/Discover/DiscoverTikTok.razor` (`@page "/discover/tiktok"`, query `?url=`); `Pages/Discover/DiscoverTikTokContent.razor`; `Pages/Discover/DiscoverTikTokLanding.razor` (`/play/tiktok`, `/discover/landing/tiktok`); reuses `YoutubePlaybackView` with `PlayerApi="tiktokPlayer"`, `PlayerVertical=true`, `AllowSaveToLibrary=false`.
**Observed behaviours:**
- Entered via a pasted TikTok link (`?url=`). Same analysis pipeline and playback UI as YouTube, but a vertical 9:16 floating player and **no Save-to-Library** bridge (TikTok transcriptions aren't persisted).
- "Open on TikTok" external link uses the source URL.
**Related models / services:** `IYouTubeApi` (shared analysis), `AnalyzedPlatform.TikTok`.
**Cross-page links:** discover/youtube-analysis, discover/chord-playback, discover/free-limits.

### discover/chord-playback
**Page title:** Chord playback and timeline
**Audience:** Member
**Authoritative sources:** `Pages/Discover/YoutubePlaybackView.razor`; `Pages/Discover/YoutubeChordGrid.razor`; `Pages/Discover/ChordTimeline.razor`; `Pages/Discover/YoutubePlayerMount.razor`.
**Observed behaviours:**
- Meta chips: play key (capo-aware "sounds in <key>"), BPM (one decimal), time signature. Beat strip dots highlight the active beat in the measure.
- **Capo stepper** (label "Capo", value "Off"/1..11, reset button). Increasing capo lowers shown chord shapes; hint: "Showing shapes to finger with a capo on fret N — the recording still sounds in <key>."
- "Chord shapes" section lists the unique chords used (capo-aware diagrams via `ChordCarousel`), collapsible.
- Floating, draggable player window (`pv-float`, drag handle `[data-pv-drag]`), minimisable; docks inline at the bottom on small screens.
- Playback sync loop polls current time every 100ms and highlights the active synced chord/beat; tap a chord to seek (`OnSeekTo`).
- N/X chord cells are rests (no diagram) — kept for rhythm/spacing.
**Related models / services:** `YouTubeTranscriptionDto.SyncedChords/Beats`, `ChordDto`, `ChordTransposer`, `ChordLabel`, chart resolver service.
**Cross-page links:** discover/youtube-analysis, compose/chord-reference, compose/viewing-chord-charts.

### discover/free-limits
**Page title:** Free daily limits and song length
**Audience:** Public
**Authoritative sources:** `FRELODYAPIs/Areas/Admin/LogicData/AnalyzedAccessService.cs`; `Pages/Discover/AnalyzedAccessSheet.razor`; `Pages/Discover/AnalyzedQuotaBadge.razor`; `Pages/Pricing.razor` (compare table + FAQ).
**Observed behaviours:**
- Free users: **2 analyses per day** (`FreeAnalyzedSongsPerDay`). Quota counts distinct (platform, videoId) unlocks since UTC midnight.
- Duration caps: free songs up to **8 minutes** (`FreeMaxDurationSeconds`); premium up to **20 minutes** (`PremiumMaxDurationSeconds`). Over the premium cap → "too-long" (no sign-in helps).
- Songs unlocked today re-play free within a **24-hour availability window** (`AvailabilityWindowHours`).
- Gate reasons surfaced to UI: `unauthenticated`, `limit-reached`, `too-long`. `AnalyzedAccessSheet` shows either a sign-in nudge ("Sign in to play your first free song", "2 free song analyses every day") or the paywall ("You've used today's N free songs", "Upgrade to keep playing", "View today's songs", "Maybe later").
- Songs already in the public library do NOT count toward the daily limit (pricing FAQ).
**Related models / services:** `AnalyzedAccessResultDto` (DailyLimit, UsedToday, Remaining, IsPremium, AlreadyUnlocked, LimitReached, Reason), `AnalyzedSongUnlock` entity, `MonetizationOptions`, `IAnalyzedAccessApi`.
**Cross-page links:** discover/todays-songs, discover/unlimited-analysis, pricing/how-premium-works.

### discover/todays-songs
**Page title:** Today's Songs
**Audience:** Premium
**Authoritative sources:** `Pages/Discover/TodaysSongs.razor` (`@page "/discover/today"`); `AnalyzedAccessService.GetTodaysSongs()`.
**Observed behaviours:**
- Title "Today's songs", subtitle "Songs you unlocked today — available for 24 hours."
- Requires sign-in (redirects to `/login?returnUrl=/discover/today`).
- Grid of unlocked songs (thumbnail, platform icon, title, expiry "Nh left"/"Nm left"/"Expired"). Tapping replays: YouTube → `/discover/{videoId}`, TikTok → `/discover/tiktok?url=…`.
- Empty state: "No songs yet today" → "Discover a song" button.
- Note: code allows any signed-in user (free or premium); the slug audience is "Premium" but the page itself is the 24h re-play list keyed off `AvailabilityWindowHours`.
**Related models / services:** `IAnalyzedAccessApi.TodaysSongs()`, `AnalyzedSongDto` (Platform, VideoId, Title, ThumbnailUrl, SourceUrl, UnlockedAt, ExpiresAt).
**Cross-page links:** discover/free-limits, discover/unlimited-analysis.

### discover/unlimited-analysis
**Page title:** Unlimited analysis (Premium)
**Audience:** Premium
**Authoritative sources:** `AnalyzedAccessService.cs` (`isPremium` short-circuits the daily cap; premium duration 20 min); `Feature.UnlimitedAnalyses`/`Feature.ExtendedSongLength`; `Pages/Pricing.razor`.
**Observed behaviours:**
- Premium (PremiumTrial / ActiveRecurring / ActiveLifetime, not expired) bypasses the 2/day cap (Remaining shown as 0/unlimited) and gets the 20-minute length cap.
- Sales copy: "Unlimited song analyses — no daily cap" and "Full-length songs, up to 20 minutes."
**Related models / services:** `BillingStatus`, `Feature`, `ProductDto`.
**Cross-page links:** pricing/plans, pricing/how-premium-works, discover/free-limits.

---

## Compose

### compose/overview
**Page title:** The song editor (Compose)
**Audience:** Member
**Authoritative sources:** `Pages/Compose/SongBoard.razor` (`@page "/compose"`, `[Authorize(Roles = Editor,Contributor)]`); board components in `Pages/Compose/BoardComponents/` (Row, Segment, AddSegment, RowActions); `Pages/Compose/TabsComponent.razor`, `SongSectionBoard.razor`.
**Observed behaviours:**
- Page heading "Song Editor". Top form: Song # (number), Song Title, Original key + working key (`KeyDropdown`) with a transpose delta button, Book/Artist (`BookArtistAdd`), and a "Save Song" button.
- Requires the org `Editor` or `Contributor` role.
- Sections/rows/segments arranged via tabbed section boards.
**Related models / services:** `SongDto`, song/section/lyric/chord APIs, `KeyDropdown`, `BookArtistAdd`.
**Cross-page links:** compose/import-a-song, compose/editing-sections, compose/adding-chords-and-lyrics, compose/saving-and-recovery.

### compose/import-a-song
**Page title:** Import a song
**Audience:** Member
**Authoritative sources:** `Pages/Compose/StudioImportModal.razor`; `Pages/Common/StudioImportPanel.razor`; `Pages/Common/StudioMonitor.razor`, `StudioSuggestions.razor`; `Pages/Play/StudioLanding.razor` (`/studio/landing`), `Pages/Studio.razor` (`/studio`).
**Observed behaviours:**
- Modal titled "Import song", subtitle "Paste, scan, upload or search — we'll structure it for the editor."
- `StudioImportPanel` accepts: pasted lyrics-with-chords, a pasted URL (web scraping), an uploaded file (OCR via image), or a library database search. Placeholder: "Paste lyrics with chords, drop a URL, upload a file, or search the library…".
- After extraction it stages a result ("Ready to apply") showing section/line/chord counts; user chooses Replace or Append into the editor.
**Related models / services:** scraping/OCR/AI pipeline (bradwarden, worshiptogether, ultimate-guitar; Tesseract OCR; Nvidia/DeepSeek AI), staged `SongDto`.
**Cross-page links:** compose/overview, compose/editing-sections.

### compose/editing-sections
**Page title:** Editing sections and rows
**Audience:** Member
**Authoritative sources:** `Pages/Compose/SongSectionBoard.razor`; `Pages/Compose/BoardComponents/{Row,Segment,AddSegment,RowActions,DropIndicator,SongDataPreview}.razor`; `Pages/Common/SortableList.razor`.
**Observed behaviours:**
- Songs are composed of sections containing rows/segments; segments can be added (`AddSegment`) and reordered (drag, `DropIndicator`/`SortableList`); `RowActions` provides per-row operations. `SongDataPreview` shows a preview.
**Related models / services:** section/row/segment DTOs.
**Cross-page links:** compose/overview, compose/adding-chords-and-lyrics.

### compose/adding-chords-and-lyrics
**Page title:** Adding chords and lyrics
**Audience:** Member
**Authoritative sources:** `Pages/Compose/ChordComponents/ChordsDropdown.razor`, `ChordCustomize.razor`; `Pages/Compose/BoardComponents/Segment.razor`.
**Observed behaviours:**
- Chords are attached to lyric segments via a chord dropdown/customize UI; works alongside the key/transpose controls on the Song Editor.
**Related models / services:** `ChordDto`, chord APIs.
**Cross-page links:** compose/editing-sections, compose/chord-charts, compose/chord-reference.

### compose/saving-and-recovery
**Page title:** Saving and session recovery
**Audience:** Member
**Authoritative sources:** `Pages/Compose/SongBoard.razor` (session-recovery alert + `cacheData`, `PreviewCache`, `RestoreFromCache`, `TryDiscardCache`, `HandleSongSave`); `Pages/useraccount/Components/RecoverySection.razor`.
**Observed behaviours:**
- On entry, if unsaved changes are cached the editor shows "Session Recovery: Unsaved changes detected. Would you like to restore your previous work?" with Preview / Restore / Discard buttons (Restore only when creating a new song).
- "Save Song" persists the song. The account profile "Recovery" tab lists auto-saved drafts ("Recovery Items" — "Automatically saved drafts from your editing sessions") with timestamps and a Preview action.
**Related models / services:** local-storage cache of song draft; recovery items DTO.
**Cross-page links:** compose/overview, account/library-and-recovery.

### compose/chord-draw
**Page title:** Draw a chord (chord-draw)
**Audience:** Public
**Authoritative sources:** `Pages/ChordCharts/ChordDrawStandalone.razor` (`@page "/chord-draw"`); `Components/ChordDraw/{ChordDrawCanvas,ChordEditor,ChordEditModeBar,ChordFingerPopover,ChordPreview,ChordSettingsPanel}.razor`.
**Observed behaviours:**
- Title "Chord diagram editor", subtitle "Tap to place a finger, drag across a fret for a barre, tap O/× above a string to mark it open or muted."
- Actions: "Download SVG" and "Download PNG" (disabled until a chart exists). Renders `ChordDrawCanvas` with `SettingsOpen=true`.
- Starts from `ChordDrawData.CreateDefault()`.
**Related models / services:** `ChordDrawData`, `chordDrawing.getSvgMarkup` / `chordDrawing.downloadPng` JS interop.
**Cross-page links:** compose/chord-charts, compose/chord-reference.

### compose/chord-charts
**Page title:** Create and edit chord charts
**Audience:** Member
**Authoritative sources:** `Pages/ChordCharts/ChordChartCreate.razor` (`@page "/chord-charts/create/{chordId}"`, `@page "/chord-charts/edit/{Id}"`); `Pages/ChordCharts/Steps/{ChordStepDetails,ChordStepDraw,ChordStepMedia,ChordStepNotes,ChordStepReview}.razor`.
**Observed behaviours:**
- A wizard ("@HeaderText Chord Chart" — Create/Edit; subtitle "Follow the steps to create your custom chord chart") with a Back button.
- Source toggle: "Draw it" (Recommended, interactive editor, `ChordSource.Drawing`) vs "Upload image" (PNG/JPG/SVG, `ChordSource.Image`). Source toggle is locked when editing an existing chart.
- Steps: Details, Draw, Media, Notes, Review.
**Related models / services:** `ChordSource` enum, chord-chart DTOs/APIs, `FormModel`.
**Cross-page links:** compose/chord-draw, compose/viewing-chord-charts, compose/chord-reference.

### compose/viewing-chord-charts
**Page title:** Viewing a chord chart
**Audience:** Public
**Authoritative sources:** `Pages/ChordCharts/ChordChartDetail.razor` (`@page "/chord-charts/{Id}"`).
**Observed behaviours:**
- Heading "Chord Chart Details"; rows for "Chord:", "Fret:" (FretPosition) and "Diagram:". Renders the drawn/standard diagram (`ChordData`) when `Source` is Drawing or Standard. Back button.
**Related models / services:** chord chart DTO (`Source`, `FretPosition`, `ChordData`).
**Cross-page links:** compose/chord-charts, compose/chord-reference.

### compose/chord-reference
**Page title:** The chord reference
**Audience:** Public
**Authoritative sources:** `Pages/Chord/ChordList.razor` (`@page "/chords"`); `Pages/Chord/ChordDetail.razor` (`@page "/chords/{Id}"`); `Pages/Chord/{ChordCard,ChordCarousel,ChordCarouselTimeline,ChordListPlaceholder}.razor`.
**Observed behaviours:**
- `/chords`: heading "Chords", search box "Search a chord...", "Add Chord" button (opens create modal). Grid of `ChordCard`s with their charts (`chartsByChordId`).
- `/chords/{Id}`: "Chord Details" / "View and manage chord information"; "Edit Chord" + back buttons.
**Related models / services:** `ChordDto`, chord + chord-chart APIs.
**Cross-page links:** compose/chord-charts, compose/viewing-chord-charts, compose/adding-chords-and-lyrics.

---

## Library

### library/songs-library
**Page title:** Your songs library
**Audience:** Member
**Authoritative sources:** `Pages/Play/SongLibrary.razor` (`@page "/songs-library"`); `Pages/Home.razor` (`@page "/songs"` renders `SongLibrary` with search/actions off); `Pages/Play/{SongList,SongListView}.razor`.
**Observed behaviours:**
- Heading "Library", search box "Search...", "Add a Song" button (when `ShowSearch`/`ShowActions` enabled). `/songs` is a public read-only listing (search and actions off); `/songs-library` is the actionable library.
- Multi-select mode (`isInSelectionMode`, `selectedSongIds`) with a toast for bulk actions.
**Related models / services:** `SongDto`, songs API, virtualization.
**Cross-page links:** library/viewing-a-song, library/playlists, compose/overview.

### library/viewing-a-song
**Page title:** Viewing and playing a song
**Audience:** Public
**Authoritative sources:** `Pages/Play/SongMain.razor` (`@page "/songs/{SongId}"`, `MainLayout`); `Pages/Play/Song.razor`; `Pages/Play/SongDetails.razor` (`@page "/player/details/{SongId}"`); `Pages/Play/Components/SongSettings.razor`; `Pages/Common/SongRating.razor`.
**Observed behaviours:**
- `/songs/{SongId}` renders the `Song` player component. Display honors the user's `SongDisplay` mode (Full / lyrics+chords / lyrics-only). Per-song settings and rating components exist.
**Related models / services:** `SongDto`, `SongDisplay` enum, songs API.
**Cross-page links:** library/song-preview, getting-started/themes-and-display, sharing/share-links.

### library/song-preview
**Page title:** Previewing a song without an account
**Audience:** Public
**Authoritative sources:** `Pages/Play/SongMain.razor` (`@page "/songs/preview"`); `Pages/Play/SongLanding.razor` (`@page "/songs/landing/{SongId}"`, `@page "/songs/landing/preview"`, `LandingLayout`).
**Observed behaviours:**
- Preview mode is detected when there's no `SongId` and the URL contains `/preview` (`IsPreview`). `/songs/landing/{SongId}` is the crawler/share-friendly landing variant (used by share redirects).
**Related models / services:** `Song` component (`IsPreview`).
**Cross-page links:** library/viewing-a-song, sharing/public-landings, sharing/share-links.

### library/playlists
**Page title:** Playlists and folders
**Audience:** Member
**Authoritative sources:** `Pages/Play/SongPlaylist.razor` (`@page "/playlists"`, `@page "/playlists/{CollectionId}"`); `Pages/Play/PlaylistsLanding.razor` (`/playlists/landing`, `/playlists/landing/{CollectionId}`); `Pages/Play/PlaylistDetail.razor` (`/playlists/{PlaylistId}/detail`); `Pages/Play/PlaylistDetailLanding.razor`; `Pages/Play/Components/{PlaylistEditModal,PlaylistFolder,PlaylistSongItem,SongSearchModal}.razor`; `Pages/Play/SongCollectionAdd.razor`.
**Observed behaviours:**
- Heading "Playlists", search "Search playlists...", "New" button (`CreateNewPlaylist`). "My Playlists" section. Folders (`PlaylistFolder`) and song items (`PlaylistSongItem`). `/playlists/landing/...` are the public/share variants.
**Related models / services:** playlist/collection DTOs, playlists API.
**Cross-page links:** library/songs-library, library/songbooks, sharing/share-links.

### library/songbooks
**Page title:** Songbooks and categories
**Audience:** Member
**Authoritative sources:** `Pages/Compose/SongBook.razor` (`@page "/songbooks"`); `Pages/Compose/SongBookCategories.razor` (`@page "/songbooks/{SongBookId}"`); `Pages/Compose/SongBookComponents/*` (SongBookCustomize, AlbumCustomize/Dropdown, ArtistCustomize/Dropdown, KeyDropdown, BookCategories/*).
**Observed behaviours:**
- `/songbooks`: heading "Song Books", cards per songbook (title, description, "Categories" link → `/songbooks/{Id}`).
- Songbooks group songs by book/artist/album/category with dedicated customize + dropdown components.
**Related models / services:** `SongBookDto`, songbook/category/artist/album APIs.
**Cross-page links:** library/songs-library, compose/overview.

---

## Pricing

### pricing/plans
**Page title:** Plans and billing
**Audience:** Public
**Authoritative sources:** `Pages/Pricing.razor` (`@page "/pricing"`, `LandingLayout`); `ProductDto`; `Feature` catalog; `BillingPeriod` (`FRELODYSHRD/Constants/BillingPeriod.cs`).
**Observed behaviours:**
- Hero "Hear it. Slow it down. Play it." Trust row: "No card to start", "2 free songs every day", "Cancel anytime".
- Billing toggle: Monthly / Yearly (−20%) / Lifetime. Premium product resolves by name "Creator Monthly/Yearly/One-Time" (falls back to any paid non-Studio plan). Free plan = "Starter" (or price 0).
- Free card: "2 song analyses every day", "Songs up to 8 minutes", then feature bullets, plus locked premium features (lock icon). CTA "Start free".
- Premium card: "Most popular" badge; price from `ICurrencyDisplayService` (local currency converted from UGX; yearly shows /mo + "Billed …/year"; "Less than CUR X/day"); CTA "Go Premium" / "Get lifetime access".
- Free vs Premium compare table: analyses/day 2 vs ∞, max length 8 min vs 20 min, replay "For 24 hours" vs "Anytime", then data-driven feature rows.
- Optional Studio plan line ("Running a band, church or team?").
- FAQ accordion (switch plans, free plan, cancellation, payment methods PayPal + PesaPal, why 2 free/day, library songs don't count, refunds — Studio has 30-day money-back).
- Legal: prices shown in local currency (converted from UGX); PayPal settled in USD; links to /terms, /privacy, /sla.
**Related models / services:** `IProductsApi.GetProducts()`, `ProductDto` (Name, Price, Currency, Period, Features), `CurrencyDisplayInfo`, `ICurrencyDisplayService`.
**Cross-page links:** pricing/how-premium-works, pricing/paypal-checkout, pricing/pesapal-checkout, administration/products-and-pricing.

### pricing/how-premium-works
**Page title:** How premium access works
**Audience:** Public
**Authoritative sources:** `AnalyzedAccessService.IsPremiumAsync`; `BillingStatus`/`BillingPeriod` enums; `Pages/Pricing.razor` `HandlePayPalSuccess` ("Premium is granted server-side at capture").
**Observed behaviours:**
- Premium granted server-side on payment capture. Premium = BillingStatus in {PremiumTrial, ActiveRecurring, ActiveLifetime} and (BillingExpiresAt null = lifetime, or future). Recurring plans lapse at expiry; lifetime never expires. SuperAdmins always premium.
- Premium removes the 2/day cap and raises the song-length cap to 20 min; unlocks the data-driven feature set.
**Related models / services:** `BillingStatus`, `BillingExpiresAt`, `Feature`.
**Cross-page links:** pricing/plans, pricing/managing-your-subscription, discover/unlimited-analysis.

### pricing/paypal-checkout
**Page title:** Pay with PayPal
**Audience:** Member
**Authoritative sources:** `Pages/Pricing.razor` checkout modal (gateway tabs) + `HandlePayPalSuccess`/`HandlePayPalError`; `Pages/Payments/PayPalButton.razor`; `IPayPalApi`.
**Observed behaviours:**
- Checkout modal "Complete your upgrade" shows both gateways as tabs. PayPal tab labeled "PayPal"; an unconfigured gateway renders disabled with a "Soon"/"coming soon" badge. Fineprint: "Charged in USD. You'll confirm securely in PayPal."
- `PayPalButton` renders the PayPal SDK buttons; states Loading ("Loading PayPal…"), Ready, Capturing ("Confirming your payment…"), Error, Unavailable ("PayPal isn't available right now."). On success: "You're Premium! 🎉 … Enjoy unlimited song analyses." then reload to `/discover`.
- PayPal availability comes from `IPayPalApi.GetConfig().Enabled` (default `payPalEnabled=false` until configured). One-time Orders v2 in USD.
**Related models / services:** `IPayPalApi`, `ProductDto.Id`.
**Cross-page links:** pricing/plans, pricing/pesapal-checkout, pricing/how-premium-works.

### pricing/pesapal-checkout
**Page title:** Pay with PesaPal
**Audience:** Member
**Authoritative sources:** `Pages/Pricing.razor` (`ContinueWithPesaPal`, `InitiatePayment`, PesaPal iframe modal); `IPesaPalApi`; `InitiatePesaPalDto`/`BillingAddress`.
**Observed behaviours:**
- PesaPal tab labeled "Mobile / Card"; button "Continue with Mobile Money / Card"; fineprint "Secured by PesaPal — mobile money, cards & bank transfer." Default `pesaPalEnabled=true` (overridden by `IPesaPalApi.GetConfig()`).
- Initiates payment with product/customer/amount/description + billing address (email, phone, country code, name, City "Kampala"), callback `…/pricing?payment=success&productId=…`, IPN `…/api/PesaPal/ipn-callback`. Opens the PesaPal gateway in an iframe modal ("Complete Your Payment").
**Related models / services:** `IPesaPalApi.InitiatePesaPalPayment`, `InitiatePesaPalDto`, `BillingAddress`, currency/country from `ICurrencyDisplayService`.
**Cross-page links:** pricing/plans, pricing/paypal-checkout, pricing/how-premium-works.

### pricing/managing-your-subscription
**Page title:** Managing your subscription
**Audience:** Premium
**Authoritative sources:** `Pages/Pricing.razor` FAQ (switch plans / cancellation); `BillingStatus` (`Cancelled`, `Expired`); `BillingExtensions`.
**Observed behaviours:**
- Per pricing FAQ: switch between Free/Monthly/Yearly/Lifetime anytime from your account; upgrades take effect immediately. Monthly/Yearly cancellable anytime (keep access to period end); Lifetime never expires (nothing to cancel).
- Note: no dedicated self-serve "manage subscription" page found in the UI; this is governed by billing status + the pricing flow. If documenting a management screen, say: No direct source found — write from the title and section context only.
**Related models / services:** `BillingStatus`, `BillingPeriod`.
**Cross-page links:** pricing/plans, pricing/how-premium-works, account/settings.

---

## Printing

### printing/print-chord-charts
**Page title:** Print chord charts
**Audience:** Premium
**Authoritative sources:** `Pages/Common/ShareDropdown.razor` (`HandlePrint`, `IsPrintAvailableAsync`, `PrintSongAsync`); `IPrintService` (injected `_printService`).
**Observed behaviours:**
- Song dropdown has a "Print" item; opens the print dialog via the print service. If unavailable: "Print Unavailable — Printing is not available on this device." Success toast "Print dialog opened!".
**Related models / services:** `IPrintService` (`IsPrintAvailableAsync`, `PrintSongAsync`), `SongDto`.
**Cross-page links:** printing/export-pdf, sharing/share-links, library/viewing-a-song.

### printing/export-pdf
**Page title:** Export songs as PDF
**Audience:** Premium
**Authoritative sources:** `Feature.PdfExport` (sales copy "Export clean chord sheets as PDF", friendly "PDF export"); `IPrintService`.
**Observed behaviours:**
- PDF export is a premium feature in the catalog. No dedicated standalone PDF-export page found in `FRELODYUI.Shared/Pages`; it surfaces as a premium capability and via the print/share path. If documenting a specific export button beyond print, say: No direct source found — write from the title and section context only.
**Related models / services:** `Feature.PdfExport`, `IPrintService`.
**Cross-page links:** printing/print-chord-charts, pricing/plans.

### printing/download-this-guide
**Page title:** Download this guide
**Audience:** Public
**Authoritative sources:** No direct source found — write from the title and section context only. (This is a docs-site feature for downloading the documentation, not a FRELODY app feature.)
**Observed behaviours:** none in product code.
**Cross-page links:** getting-started/how-to-use-this-guide, help/faq.

---

## Sharing

### sharing/share-links
**Page title:** Share links
**Audience:** Member
**Authoritative sources:** `Pages/Common/ShareDropdown.razor` (`HandleShareLink`); `Services/ShareService.cs` / `IShareService`; `RefitApis/IShareApi.cs`; `Controllers/ShareController.cs` (`api/Share/GenerateShareLink`, `GetSharedSong`, `GetSharedPlaylist`, `RevokeShareLink`); `Areas/Admin/LogicData/ShareLinkService.cs`; `Models/ShareLink.cs`.
**Observed behaviours:**
- "Share Link" item copies a share URL to the clipboard ("Share link copied to clipboard!"). Generates one via `ShareService.GenerateShareLinkAsync(songId)` if not already provided.
- Dropdown also has "Customize" (edit, sign-in gated) and, for owner+premium or SuperAdmin, a visibility toggle Private/Visible (`MarkSongAccessStatus`, `Access` enum).
- Share links carry an OG snapshot (title/description/image) and optional expiry.
**Related models / services:** `ShareLink` (ShareToken, SongId/PlaylistId, OgTitle/OgDescription/OgImagePath/OgHtml, ExpiresAt), `ShareLinkCreateDto`/`ShareLinkDto`, `IClipboardService`, `Access` enum.
**Cross-page links:** sharing/public-landings, sharing/social-cards, library/viewing-a-song.

### sharing/public-landings
**Page title:** Public song and playlist pages
**Audience:** Public
**Authoritative sources:** `Controllers/ShareLandingController.cs` (`/shared/{token}`, `/shared/playlist/{token}`); redirect targets `/songs/landing/{SongId}` and `/playlists/landing/{PlaylistId}/detail`; `Pages/Play/SongLanding.razor`, `PlaylistDetailLanding.razor`.
**Observed behaviours:**
- `/shared/{token}` (and `/shared/playlist/{token}`) serve crawler-friendly HTML with OG/Twitter meta from the `ShareLink` snapshot, then redirect real browsers (meta-refresh + JS `location.replace`, `<noscript>` link "Open in Frelody →") into the Blazor landing route.
- Expired link → "This share link has expired"; missing → "Shared content not found". Cache-Control public, max-age=300.
**Related models / services:** `ShareLink`, `ShareLandingOptions` (PublicBaseUrl, AppBaseUrl, SiteName "Frelody").
**Cross-page links:** sharing/share-links, sharing/social-cards, library/song-preview.

### sharing/social-cards
**Page title:** Social cards (Open Graph)
**Audience:** Public
**Authoritative sources:** `ShareLandingController.cs` (OG/Twitter meta build); `ShareLinkService.cs` (`_ogCardService.RenderPngAsync(content, token)` → `OgImagePath`); `ShareLink.OgImagePath`.
**Observed behaviours:**
- Each share link gets a rendered PNG card (1200×630) stored at `OgImagePath` and emitted as `og:image`/`twitter:image`. Meta includes og:type `music.song`/`music.playlist`, og:site_name (default "Frelody"), title/description from the snapshot, twitter card `summary_large_image` when an image exists.
- Cards render correctly on WhatsApp, iMessage, Facebook, Twitter, LinkedIn, Slack, Discord without running JS.
**Related models / services:** OG card render service (`RenderPngAsync`), `ShareLink` OG fields.
**Cross-page links:** sharing/share-links, sharing/public-landings.

---

## Account

### account/your-profile
**Page title:** Your profile
**Audience:** Member
**Authoritative sources:** `Pages/useraccount/UserProfile.razor` (`@page "/user-profile"`, `@page "/user-profile/{userId}"`); `Components/{ViewProfileHeader,EditProfileHeader}.razor`; `Pages/useraccount/UserDetail.razor` (`/users/{UserId}`).
**Observed behaviours:**
- Profile header (view/edit) plus tabbed nav: Library, Recovery, Settings, Account. Edit profile via `EditProfileHeader` (`OnSave`/`OnCancel`, `IsSubmitting`).
**Related models / services:** `UpdateUserProfileOutDto`/user profile DTOs, `IUsersApi.GetUserProfile`.
**Cross-page links:** account/library-and-recovery, account/settings, account/password-and-security.

### account/library-and-recovery
**Page title:** Library and recovery
**Audience:** Member
**Authoritative sources:** `Pages/useraccount/Components/LibrarySection.razor`; `Pages/useraccount/Components/RecoverySection.razor`.
**Observed behaviours:**
- Profile "Library" tab lists the user's content; "Recovery" tab lists "Recovery Items" — "Automatically saved drafts from your editing sessions" with timestamps and a Preview action (linked to Compose session recovery).
**Related models / services:** recovery item DTO, songs/library APIs.
**Cross-page links:** account/your-profile, compose/saving-and-recovery, library/songs-library.

### account/settings
**Page title:** Settings
**Audience:** Member
**Authoritative sources:** `Pages/useraccount/Components/SettingsSection.razor`.
**Observed behaviours:**
- Appearance: Theme `<select>` (`Theme` enum) and a "Notify" switch. Display: song "Mode" segmented control (Full = `LyricsAndChordsAndCharts`, plus lyrics+chords / lyrics-only via `SongDisplay`).
**Related models / services:** user settings DTO, `Theme` / `SongDisplay` enums.
**Cross-page links:** getting-started/themes-and-display, account/your-profile.

### account/password-and-security
**Page title:** Password and security
**Audience:** Member
**Authoritative sources:** `Pages/useraccount/Components/AccountSection.razor`; `Pages/useraccount/Components/ChangePasswordModal.razor`; `Pages/ResetPassword.razor` (`@page "/resetpassword/{Token}"`); `Pages/Auth/MustChangePassword.razor`.
**Observed behaviours:**
- Account > Security: "Password — Change your account password" (opens Change Password modal). "Two-Factor Authentication — Add an extra layer of security (Coming Soon)" toggle is disabled.
- Password reset via emailed token route `/resetpassword/{Token}`; forced change at `/account/change-password-required`.
**Related models / services:** auth/password APIs, email OTP.
**Cross-page links:** getting-started/sign-in-and-sign-out, account/settings.

---

## Organizations

### organizations/overview
**Page title:** Organizations overview
**Audience:** Member
**Authoritative sources:** `Pages/Organizations/MyOrganization.razor` (`@page "/organizations/me"`); `FRELODYSHRD/Constants/UserRoles.cs` (two-layer role model); `Components/Admin/AuthGuard.razor`, `OrgSwitchWarningModal.razor`.
**Observed behaviours:**
- `/organizations/me`: if no org — "You don't belong to an organization" with "Create organization" / "Join with ID". If a member — shows org name, industry, member count, country, "Since <date>".
- Org = a shared workspace for a team's music; roles Owner>Admin>Manager>Editor>Contributor>Viewer>Guest.
**Related models / services:** `IOrganizationsApi`, org DTO (Name, Industry, Country, MemberCount, DateCreated).
**Cross-page links:** organizations/create-an-organization, organizations/join-an-organization, organizations/members-and-roles.

### organizations/create-an-organization
**Page title:** Create an organization
**Audience:** Member
**Authoritative sources:** `Pages/Organizations/CreateOrganization.razor` (`@page "/organizations/create"`, wrapped in `AuthGuard`).
**Observed behaviours:**
- Title "Create your organization", subtitle "A workspace for your team's music." Fields: Organization name * (e.g. "Riverside Worship Band"), Industry, Country.
- If already in an org: "You're already a member of <Name>. Leave it first if you want to create a new one." + "View my organization".
**Related models / services:** `IOrganizationsApi`, create-org DTO (`_dto.Name/Industry/Country`).
**Cross-page links:** organizations/overview, organizations/join-an-organization.

### organizations/join-an-organization
**Page title:** Join an organization
**Audience:** Member
**Authoritative sources:** `Pages/Organizations/JoinOrganization.razor` (`@page "/organizations/join"`, `@page "/organizations/join/{TargetId}"`, `AuthGuard`); `Components/Admin/OrgSwitchWarningModal.razor`.
**Observed behaviours:**
- Title "Join an organization", subtitle "Switch your active workspace." Field "Organization ID" (paste from invite); help "Ask the org owner for the ID, or open the invite link they sent." "Continue" previews then confirms via the org-switch warning modal.
**Related models / services:** `IOrganizationsApi`, org-switch warning DTO.
**Cross-page links:** organizations/overview, organizations/members-and-roles.

### organizations/members-and-roles
**Page title:** Members, roles and ranks
**Audience:** Admin
**Authoritative sources:** `Pages/Admin/OrgMembersPage.razor` (`@page "/admin/members"`); `Components/Admin/{OrgUserDisplay,OrgUserAddEdit,OrgRoleAuthorizeView}.razor`; `FRELODYSHRD/Constants/UserRoles.cs` (`OrgRoleRank`, `CanManage`).
**Observed behaviours:**
- Gated to Owner/Admin/Manager (else "Only Owners, Admins, and Managers can manage members."). Title "Members — Invite, manage roles, and monitor membership."
- Operations: add/invite member, edit roles, resend credentials, disable/enable member.
- Role ranks: Owner 70 > Admin 60 > Manager 50 > Editor 40 > Contributor 30 > Viewer 20 > Guest 10; a caller may only manage targets they strictly outrank (`CanManage`).
**Related models / services:** `IOrganizationsApi`, member DTOs, `UserRoles`.
**Cross-page links:** administration/manage-members, organizations/overview, administration/org-dashboard.

### organizations/projects
**Page title:** Projects
**Audience:** Admin
**Authoritative sources:** `Pages/Admin/OrgProjectsPage.razor` (`@page "/admin/projects"`, `AdminLayout`); `Components/Admin/AdminTabs.razor`.
**Observed behaviours:**
- Gated to Owner/Admin/Manager/Editor. Title "Projects — All content created under <org>." Tabs for Songs / Playlists / Songbooks, each linking to the respective app section (/songs, /playlists, /songbooks).
**Related models / services:** `IOrganizationsApi`.
**Cross-page links:** administration/org-dashboard, library/songs-library, library/playlists, library/songbooks.

---

## Administration

### administration/org-dashboard
**Page title:** Organization dashboard
**Audience:** Admin
**Authoritative sources:** `Pages/Admin/OrgDashboard.razor` (`@page "/admin/dashboard"`, `AdminLayout`); `Components/Admin/{MetricCard,AuthGuard,OrgRoleAuthorizeView}.razor`; `Components/PlayHistoryChart.razor`.
**Observed behaviours:**
- Gated to Owner/Admin/Manager (else "You need the Owner, Admin, or Manager role inside an organization to view this page."). Header shows org name/industry, an "Admin" pill, and links to Members / Projects / Plans.
- Metric cards; brand-new orgs get a "Welcome to <org>" getting-started panel (Invite members / Add a song / Create a playlist).
**Related models / services:** `IOrganizationsApi`, org metrics DTO.
**Cross-page links:** administration/manage-members, organizations/projects, administration/products-and-pricing.

### administration/manage-members
**Page title:** Manage members
**Audience:** Admin
**Authoritative sources:** `Pages/Admin/OrgMembersPage.razor` (`@page "/admin/members"`) — same page as organizations/members-and-roles; `Components/Admin/OrgUserAddEdit.razor`, `ConfirmDialog.razor`.
**Observed behaviours:**
- See organizations/members-and-roles. Adds the "Change member roles" confirm dialog (`ConfirmLabel "Save changes"`, icon `bi-shield-lock`), invite-vs-create flows (`HandleInvite`/`HandleCreate`), and disable/enable/resend actions.
**Related models / services:** `IOrganizationsApi`, member DTOs.
**Cross-page links:** organizations/members-and-roles, administration/org-dashboard.

### administration/tenants
**Page title:** Tenants
**Audience:** Admin
**Authoritative sources:** `Pages/Admin/TenantsList.razor` (`@page "/admin/tenants"`); `ITenantsApi`.
**Observed behaviours:**
- Title "Tenant Management — Manage application tenants." Search box "Search tenants..." with clear button. Error alerts. Virtualized list.
**Related models / services:** `ITenantsApi`, tenant DTOs.
**Cross-page links:** administration/org-dashboard, administration/products-and-pricing.

### administration/products-and-pricing
**Page title:** Products and pricing (SuperAdmin)
**Audience:** Admin
**Authoritative sources:** `Pages/Admin/AdminProductsPage.razor` (`@page "/admin/products"`, `AdminLayout`); `IProductsApi`; `Feature` catalog; `ProductDto`.
**Observed behaviours:**
- Gated to the **SuperAdmin** platform role (not org admin) — else "Only platform administrators can manage plans and pricing." Does NOT require an organization.
- Title "Plans & pricing — Edit the plans shown on the public pricing page. Changes are live immediately." "Add plan" button; skeleton/empty states; per-plan editor uses the same `Feature` enum as the public pricing page (single source of truth via `FeatureExtensions.ToFriendlyString`).
**Related models / services:** `IProductsApi` (GetProducts/create/update), `ProductDto` (Name, Price, Currency, Period, Features), `Feature`.
**Cross-page links:** pricing/plans, pricing/how-premium-works, administration/org-dashboard.

---

## Help

### help/faq
**Page title:** Frequently asked questions
**Audience:** Public
**Authoritative sources:** `Pages/Pricing.razor` FAQ accordion (the only in-product FAQ found).
**Observed behaviours:** Real Q&As: "Can I switch plans later?", "Is there really a free plan?" (2 analyzed songs/day forever, no card), "How does cancellation work?", "What payment methods do you accept?" (PayPal worldwide + PesaPal mobile money/cards/bank across Africa), "Why only 2 free songs a day?", "Do songs already in FRELODY count toward my daily limit?" (no — only YouTube/TikTok analyses count), "Do you offer refunds?" (Studio 30-day money-back; others contact support).
**Related models / services:** none.
**Cross-page links:** pricing/plans, discover/free-limits, help/get-support, help/glossary.

### help/glossary
**Page title:** Glossary
**Audience:** Public
**Authoritative sources:** No direct source found — write from the title and section context only. Build from real product terms used elsewhere in this brief: analysis/analyzed song, synced chords, beat strip, capo, key/transpose, chord chart, chord-draw, songbook, playlist, organization, tenant, billing status/period, share link, OG card.
**Observed behaviours:** none as a page.
**Cross-page links:** help/faq, discover/chord-playback, compose/chord-reference.

### help/get-support
**Page title:** Get support
**Audience:** Public
**Authoritative sources:** `Pages/Common/FeedbackDialog.razor` (in-app feedback); pricing FAQ ("reach out to our support team"); `Feature.PrioritySupport`. No dedicated standalone support/contact page found.
**Observed behaviours:**
- In-app feedback dialog exists (`FeedbackDialog`). Premium plans advertise "Priority support when you need it." For a contact-channel page beyond these, say: No direct source found — write from the title and section context only.
**Related models / services:** feedback DTO, `Feature.PrioritySupport`.
**Cross-page links:** help/faq, pricing/plans.
