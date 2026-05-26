# YouTube Discover Feature — Implementation Plan

A synchronized YouTube song discovery and chord recognition feature for FRELODY.  
Users search YouTube, select a video, and play along with synchronized chord + beat visualization.

---

## Architecture Decisions

### Two separate user journeys — Studio and Discover stay independent

- **Studio** (`/studio`) → text/file/URL lyrics extraction → saveable chord chart. Unchanged.
- **Discover** (`/discover`) → YouTube search → audio analysis → synchronized playback session.

The bridge between them: after analysis on Discover, the user can **Save to Library** — this maps the YouTube transcription to `SimpleSongCreateDto` and calls the existing `POST /api/songs`.

### Python ML backend as Docker sidecar (no .NET rewrite)

ChordMiniApp's Flask backend (`D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\python_backend`) is already containerized. It runs as a new `chordmini-backend` service in `docker-compose.yml`. The .NET API calls it internally via `http://chordmini-backend:5001`. No ML code is ported to .NET.

### SQL Server for all storage — no Firebase

All caching (YouTube video metadata, chord+beat transcriptions) uses SQL Server via EF Core migrations in `FRELODYLIB`. The `frelody_media` Docker volume (already defined in `docker-compose.yml:181`) is used for temporary audio files during analysis.

### YoutubeExplode NuGet for search

No YouTube API key required. `YoutubeExplode` handles search, video metadata, and audio stream URL extraction entirely client-side. yt-dlp inside the Python backend handles the actual audio download for analysis.

### Blazor WASM for synchronized playback

The YouTube IFrame Player API is loaded via JS interop. A `setInterval` at 100ms polls `player.getCurrentTime()` and updates a Blazor `currentTimeSeconds` float. The chord grid highlights the active chord by binary-searching the sorted `SyncedChords` array. All playback sync logic runs in the browser — zero server round trips while playing.

---

## Input Priority Hierarchy

```
youtube.com/watch?v=...  ──►  Navigate to /discover?v={videoId}   (never scrape)
youtu.be/...             ──►  Same

bradwarden.com/...       ──►  Web scraping  (existing, unchanged)
worshiptogether.com/...  ──►  Web scraping  (existing, unchanged)
other http/https URL     ──►  Generic scrape attempt  (existing)

Text in Studio search    ──►  Database results first
                              Footer link: "Search YouTube →" → /discover?q={query}

[Am]Amazing...lyrics     ──►  Text extraction only. NEVER touch YouTube.

File upload              ──►  File extraction  (existing, unchanged)
Camera scan              ──►  OCR extraction   (existing, unchanged)
```

---

## Pre-work: Remove Unwired YouTube Dead Code

The `SuggestionSource.YouTube` enum value was added prematurely. The Studio suggestion flow has no YouTube pathway wired. Remove it now; it will be re-introduced properly in Phase 5.

### Files to change

**`FRELODYUI/FRELODYUI.Shared/Models/StudioSuggestion.cs:8`**
Remove the `YouTube` enum value from `SuggestionSource`.

**`FRELODYUI/FRELODYUI.Shared/Pages/Common/StudioSuggestions.razor.css:111-113`**
Remove `.ss-badge--youtube { ... }` block. The CSS tokens in `app.css` (`--badge-youtube-bg/text`) can stay — they are harmless and will be needed in Phase 5.

**`FRELODYUI/FRELODYUI.Shared/Pages/Studio.razor:190`**
The `SuggestionSource.Database` guard here needs no change — it just won't have a YouTube branch to trip over anymore.

---

## Phase 1 — YouTube Search + Video Metadata

### Goal
Users can open `/discover`, type a song title, see YouTube video cards (thumbnail, title, channel, duration), and click a card to proceed to analysis.

### Infrastructure

**`docker-compose.yml`** — Add `chordmini-backend` service after `frelody-api`:

```yaml
chordmini-backend:
  build:
    context: D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\python_backend
    dockerfile: Dockerfile
  container_name: frelody-chordmini
  ports:
    - "5001:5001"
  environment:
    FLASK_ENV: production
    MAX_CONTENT_LENGTH_MB: "150"
  volumes:
    - frelody_media:/app/media
    - chordmini_models:/app/models
  networks:
    - frelody-net

volumes:
  chordmini_models:   # ML model files; mount from host in prod
```

Add env var to `frelody-api` service:
```yaml
ChordMini__BaseUrl: "http://chordmini-backend:5001"
```

Reference: ChordMiniApp Dockerfile at `D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\python_backend\Dockerfile`

### NuGet packages

Add to `FRELODYAPIs/FRELODYAPIs.csproj`:
- `YoutubeExplode` (latest stable)

### Database migration

New entities in `FRELODYLIB/Models/`:

**`YouTubeVideo.cs`** (new file)
```
Id: int PK
VideoId: string(11) — YouTube video ID, unique indexed
Title: string
ChannelTitle: string
ThumbnailUrl: string
DurationSeconds: int
CreatedAt: DateTimeOffset
```

**`YouTubeTranscription.cs`** (new file)
```
Id: int PK
VideoId: string(11) FK → YouTubeVideo.VideoId
BeatModel: string       — e.g. "beat-transformer"
ChordModel: string      — e.g. "chord-cnn-lstm"
ChordDict: string       — e.g. "full"
BeatsJson: string       — JSON array of float seconds
ChordsJson: string      — JSON array of {time, chord, confidence}
SyncedChordsJson: string — JSON array of {time, chord, beatIndex}
Bpm: float?
TimeSignature: string?  — e.g. "4/4"
KeySignature: string?   — e.g. "C major"
TotalProcessingSeconds: float
CreatedAt: DateTimeOffset
Composite unique index: (VideoId, BeatModel, ChordModel, ChordDict)
```

Register both in `FRELODYLIB/SongDbContext.cs` → `OnModelCreating`.  
Run migration from `FRELODYLIB/`: `dotnet ef migrations add AddYouTubeDiscover --startup-project ../FRELODYAPIs`

### API — YouTubeController

New file: `FRELODYAPIs/Controllers/YouTubeController.cs`

Endpoints:
```
GET  /api/youtube/search?q={query}&limit={n}
     → calls YoutubeExplode, returns List<YouTubeVideoDto>
     → caches results in YouTubeVideo table if not present

GET  /api/youtube/videos/{videoId}
     → returns single YouTubeVideoDto from cache or YoutubeExplode lookup
```

**`YouTubeVideoDto`** (new file in `FRELODYSHRD/Dtos/`):
```
VideoId, Title, ChannelTitle, ThumbnailUrl, DurationSeconds, YouTubeUrl
```

Follow existing controller pattern in `FRELODYAPIs/Controllers/SongsController.cs` for DI, auth, and response patterns.

### Refit interface

New file: `FRELODYUI/FRELODYUI.Shared/RefitApis/IYouTubeApi.cs`

```csharp
[Get("/api/youtube/search")]
Task<ApiResponse<List<YouTubeVideoDto>>> SearchAsync([Query] string q, [Query] int limit = 10);

[Get("/api/youtube/videos/{videoId}")]
Task<ApiResponse<YouTubeVideoDto>> GetVideoAsync(string videoId);
```

Register in both `FRELODYUI.Web/Program.cs` and `FRELODYUI.Web.Client/Program.cs` following the existing Refit pattern (search for `AddRefitClient` in both files).

### UI — /discover page

New files:
- `FRELODYUI/FRELODYUI.Shared/Pages/Discover.razor` — `@page "/discover"`
- `FRELODYUI/FRELODYUI.Shared/Pages/Discover.razor.css`

The page structure:

```
Discover.razor
  ├── search bar (query input + submit)
  ├── YoutubeResultGrid.razor  — thumbnail cards
  │     Each card: thumbnail, title, channel, duration chip
  │     Click → navigate to /discover/{videoId} or set selected state
  └── empty/loading states with .k-shimmer placeholders
```

New placeholder: `FRELODYUI/FRELODYUI.Shared/Pages/PlaceHolders/YoutubeResultPlaceholder.razor`  
Follow existing placeholder pattern from `Pages/PlaceHolders/` directory.

**NavMenu addition** (`FRELODYUI/FRELODYUI.Shared/Layout/NavMenu.razor`):
Add `/discover` link alongside `/studio` (line ~35). Use `bi-youtube` or `bi-music-note-beamed` icon.

---

## Phase 2 — Audio Analysis Pipeline

### Goal
Selected video triggers chord + beat recognition via the Python backend. Results cached in SQL. Analysis status shown with progress stages.

### API additions to YouTubeController

```
POST /api/youtube/analyze
     Body: { videoId, beatModel, chordModel, chordDict, forceRefresh }
     → Check cache (YouTubeTranscription table) — return immediately if hit
     → If miss: call chordmini-backend to extract audio + recognize chords + detect beats
     → Store result in YouTubeTranscription table
     → Return YouTubeTranscriptionDto

GET  /api/youtube/transcriptions/{videoId}
     Query: ?beatModel=&chordModel=&chordDict=
     → Return cached YouTubeTranscriptionDto or 404
```

**`YouTubeTranscriptionDto`** (new in `FRELODYSHRD/Dtos/`):
```
Id, VideoId, BeatModel, ChordModel, ChordDict,
Beats: List<float>,
Chords: List<ChordEventDto>,        // {Time, Chord, Confidence}
SyncedChords: List<SyncedChordDto>, // {Time, Chord, BeatIndex}
Bpm, TimeSignature, KeySignature,
TotalProcessingSeconds, CreatedAt
```

### ChordMini client service

New file: `FRELODYAPIs/Services/ChordMini/IChordMiniService.cs` + `ChordMiniService.cs`

Calls the Python backend internally:
- `POST http://chordmini-backend:5001/api/ytdlp/extract` → get audio file path
- `POST http://chordmini-backend:5001/api/detect-beats` → beat array
- `POST http://chordmini-backend:5001/api/recognize-chords` → chord array

Reference ChordMiniApp source for exact request/response shapes:
- Beat endpoint: `D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\python_backend\blueprints\beats\routes.py:40-142`
- Chord endpoint: `D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\python_backend\blueprints\chords\routes.py:43-142`
- Audio extraction: `D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\python_backend\blueprints\audio\validators.py:13-72`

Register `IChordMiniService` in `FRELODYAPIs/Program.cs` with named `HttpClient` pointing to `ChordMini__BaseUrl`.

### Refit interface additions

Add to `IYouTubeApi.cs`:
```csharp
[Post("/api/youtube/analyze")]
Task<ApiResponse<YouTubeTranscriptionDto>> AnalyzeAsync([Body] YouTubeAnalyzeRequest request);

[Get("/api/youtube/transcriptions/{videoId}")]
Task<ApiResponse<YouTubeTranscriptionDto>> GetTranscriptionAsync(string videoId,
    [Query] string beatModel, [Query] string chordModel);
```

**`YouTubeAnalyzeRequest`** (new DTO):
```
VideoId, BeatModel (default "beat-transformer"), ChordModel (default "chord-cnn-lstm"),
ChordDict (default "full"), ForceRefresh (default false)
```

### UI — Analysis state on /discover/{videoId}

New route variant: `@page "/discover/{VideoId}"` (or query param on Discover.razor)

States to render:
1. **Idle** — video card shown, "Analyze" button
2. **Extracting audio** — spinner with "Extracting audio…"
3. **Detecting beats** — spinner with "Detecting beats…"
4. **Recognizing chords** — spinner with "Recognizing chords…" + estimated wait note
5. **Complete** → transition to Phase 3 playback view
6. **Error** — retry option

Use staggered `bounce-dot` animation pattern from ChordMini (three dots scaling 0→1 in sequence).  
Add `--k-transition-beat: 80ms ease` to `app.css` token layer for use in Phase 3.

---

## Phase 3 — Synchronized Playback View

### Goal
Chord grid + beat timeline are live-highlighted in sync with the embedded YouTube video.

### New component: YoutubePlaybackView

New files:
- `FRELODYUI/FRELODYUI.Shared/Pages/Discover/YoutubePlaybackView.razor`
- `FRELODYUI/FRELODYUI.Shared/Pages/Discover/YoutubePlaybackView.razor.css`

Layout (mobile-first):
```
┌─────────────────────────────────┐
│   YouTube IFrame player         │  ← 16:9, full width on mobile
├─────────────────────────────────┤
│   Key   BPM   Time sig          │  ← metadata chips
├─────────────────────────────────┤
│   Beat timeline strip           │  ← pulsing dots on each beat
├─────────────────────────────────┤
│   Chord grid                    │  ← grid-cols-{4} default, active chord highlighted
│   C    G    Am   F              │
│   [active:blue ring]            │
├─────────────────────────────────┤
│   [Save to Library]  [Share]    │
└─────────────────────────────────┘
```

Desktop: player and chord grid side by side.

### JS interop for YouTube IFrame API

New file: `FRELODYUI/FRELODYUI.Shared/wwwroot/js/youtube-player.js`

```javascript
// Initializes YouTube IFrame API, exposes:
// - window.ytPlayer.play(videoId, elementId)
// - window.ytPlayer.getCurrentTime() → float
// - window.ytPlayer.onReady callback
// - window.ytPlayer.onStateChange callback
```

Reference ChordMiniApp's existing IFrame integration for the JS pattern:  
`D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\src\components\` (look for YouTubePlayer component).

Register the script in `FRELODYUI/FRELODYUI.Web/wwwroot/index.html` and `FRELODYUI.Web.Client/wwwroot/index.html`.

### Blazor sync loop

In `YoutubePlaybackView.razor`:
- `DotNetObjectReference` passed to JS for `onStateChange` callback
- When player state = Playing: start a `PeriodicTimer` at 100ms intervals calling `player.getCurrentTime()` via JS interop
- `currentTimeSeconds` float drives chord highlighting — binary search `SyncedChords` array for the last entry where `entry.Time <= currentTimeSeconds`
- `StateHasChanged()` called only when active chord index changes (not every 100ms tick)

### Chord grid

New component: `YoutubeChordGrid.razor`

- Displays all unique chords in order of first appearance
- Active chord has `--k-chord-active-bg` / `--k-chord-active-border` ring
- Tapping a chord cell seeks the video to the first occurrence of that chord
- Time signature determines grid columns: `grid-cols-4` for 4/4, `grid-cols-3` for 3/4, etc.

New tokens to add to `app.css` in both light and dark blocks:
```css
/* light */
--k-chord-active-bg: #eef2ff;
--k-chord-active-border: #4f46e5;
--k-beat-pulse: #3b82f6;
--k-transition-beat: 80ms ease;
/* dark */
--k-chord-active-bg: #1e1b4b;
--k-chord-active-border: #818cf8;
--k-beat-pulse: #60a5fa;
```

---

## Phase 4 — Save to Library Bridge

### Goal
After analysis, the user can save the YouTube song as a chord chart in their FRELODY library.

### Mapping logic

`YouTubeTranscriptionDto` → `SimpleSongCreateDto`:
- `Title` from `YouTubeVideoDto.Title`
- `SongLyrics`: each `SyncedChord` entry becomes a `SegmentCreateDto` with `ChordName = chord`, `Lyric = ""`, `LineNumber` and `LyricOrder` derived from beat grouping
- `Key = KeySignature` if available

This mapping lives in `FRELODYAPIs/Services/YouTubeSongMapper.cs` (new file).

### API endpoint

Add to `YouTubeController.cs`:
```
POST /api/youtube/save-to-library
     Body: { videoId, beatModel, chordModel, title? }
     → Load cached transcription
     → Map to SimpleSongCreateDto
     → Call existing SongsService.CreateSong()
     → Return Song Id + slug for navigation
```

### UI

"Save to Library" button in `YoutubePlaybackView.razor`:
- Authenticated: calls save endpoint → navigates to `/songs/{slug}`
- Unauthenticated: navigates to `/login?returnUrl=/discover/{videoId}`

---

## Phase 5 — Studio Integration

### Goal
Studio users who paste a YouTube URL are redirected to Discover instead of hitting a scraping 404. YouTube results appear as a secondary suggestion lane in the Studio search.

### StudioImportPanel URL detection

**`FRELODYUI/FRELODYUI.Shared/Pages/Common/StudioImportPanel.razor`** — in the URL detection block (around line 437):

Add YouTube URL pattern check before the generic scraping path:
```csharp
if (IsYouTubeUrl(detectedUrl, out var videoId))
{
    _navManager.NavigateTo($"/discover?v={videoId}");
    return;
}
```

`IsYouTubeUrl()` — private helper parsing `youtube.com/watch?v=`, `youtu.be/`, `music.youtube.com/watch?v=`.

### Studio suggestions — YouTube footer link

**`FRELODYUI/FRELODYUI.Shared/Pages/Common/StudioSuggestions.razor`**

When `Suggestions.Any()` and a `Query` is present, add a footer row below the song list:
```html
<div class="ss-footer-link" @onclick="OnSearchYouTube">
  <i class="bi bi-youtube"></i>
  Search "@Query" on YouTube →
</div>
```

Clicking navigates to `/discover?q={Query}`.

Re-add `SuggestionSource.YouTube` to the enum and `.ss-badge--youtube` CSS only if YouTube results will be shown inline in the suggestion panel (deferred decision — start with footer link only).

---

## New CSS Tokens (app.css)

Add to both `:root, [data-bs-theme="light"]` block and `[data-bs-theme="dark"]` block.

```css
/* Beat/chord playback surfaces */
--k-chord-active-bg        (light: #eef2ff       dark: #1e1b4b)
--k-chord-active-border    (light: #4f46e5       dark: #818cf8)
--k-beat-pulse             (light: #3b82f6       dark: #60a5fa)
--k-transition-beat: 80ms ease;   /* theme-agnostic, light block only */
```

These go after `--k-transition-slow` in the existing token block (`app.css` around line 295).

---

## New Files Reference

### FRELODYLIB (models + migrations)
- `Models/YouTubeVideo.cs`
- `Models/YouTubeTranscription.cs`
- `Migrations/` — generated migration: `AddYouTubeDiscover`

### FRELODYSHRD (DTOs)
- `Dtos/YouTubeVideoDto.cs`
- `Dtos/YouTubeTranscriptionDto.cs`
- `Dtos/ChordEventDto.cs`
- `Dtos/SyncedChordDto.cs`
- `Dtos/CreateDtos/YouTubeAnalyzeRequest.cs`

### FRELODYAPIs (backend)
- `Controllers/YouTubeController.cs`
- `Services/ChordMini/IChordMiniService.cs`
- `Services/ChordMini/ChordMiniService.cs`
- `Services/YouTubeSongMapper.cs`

### FRELODYUI.Shared (UI)
- `RefitApis/IYouTubeApi.cs`
- `Pages/Discover.razor` + `.razor.css`
- `Pages/Discover/YoutubePlaybackView.razor` + `.razor.css`
- `Pages/Discover/YoutubeChordGrid.razor` + `.razor.css`
- `Pages/Discover/YoutubeResultGrid.razor` + `.razor.css`
- `Pages/PlaceHolders/YoutubeResultPlaceholder.razor`
- `wwwroot/js/youtube-player.js`

---

## Files Modified Reference

| File | Change |
|------|--------|
| `docker-compose.yml` | Add `chordmini-backend` service + `ChordMini__BaseUrl` env var to `frelody-api` |
| `FRELODYAPIs/Program.cs` | Register `IChordMiniService`, `IYouTubeApi` HttpClient |
| `FRELODYUI.Web/Program.cs` | Register Refit `IYouTubeApi` client |
| `FRELODYUI.Web.Client/Program.cs` | Register Refit `IYouTubeApi` client |
| `FRELODYLIB/SongDbContext.cs` | Add `YouTubeVideos` + `YouTubeTranscriptions` DbSets + model config |
| `FRELODYUI.Shared/Models/StudioSuggestion.cs` | Remove `YouTube` from `SuggestionSource` enum (Phase 5: restore) |
| `FRELODYUI.Shared/Pages/Common/StudioSuggestions.razor` | Phase 5: Add YouTube footer link |
| `FRELODYUI.Shared/Pages/Common/StudioSuggestions.razor.css` | Remove `.ss-badge--youtube` block (Phase 5: restore) |
| `FRELODYUI.Shared/Pages/Common/StudioImportPanel.razor` | Phase 5: Add YouTube URL detection + redirect |
| `FRELODYUI.Shared/Layout/NavMenu.razor` | Add `/discover` nav link |
| `FRELODYUI.Shared/wwwroot/app.css` | Add `--k-chord-active-*`, `--k-beat-pulse`, `--k-transition-beat` tokens |

---

## Reference — ChordMiniApp Source Locations

> **READ-ONLY reference.** ChordMiniApp (`D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp`) is a separate project kept alongside FRELODY solely so we can read its source without going to GitHub. **Never write to, commit in, or modify any file under that path.** It exists here for reference only.

For porting guidance, key ChordMiniApp files at `D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp`:

| What | ChordMiniApp File |
|------|-------------------|
| YouTube search API route | `src/app/api/search-youtube/route.ts:1-229` |
| Audio extraction validator | `python_backend/blueprints/audio/validators.py:13-72` |
| Beat detection endpoint | `python_backend/blueprints/beats/routes.py:40-142` |
| Chord recognition endpoint | `python_backend/blueprints/chords/routes.py:43-142` |
| Chord-CNN-LSTM model wrapper | `python_backend/services/detectors/chord_cnn_lstm_detector.py` |
| Beat-Transformer wrapper | `python_backend/services/detectors/beat_transformer_detector.py` |
| SongFormer segmentation (Phase 6+) | `python_backend/services/audio/songformer_service.py` |
| Python requirements | `python_backend/requirements.txt` |
| Python Dockerfile | `python_backend/Dockerfile` |
| ChordMini global CSS tokens | `src/app/globals.css:1-200` |
| Chord sync state management | `src/contexts/ProcessingContext.tsx` |

---

## Deferred — Phase 6+ (not in scope now)

- **SongFormer segmentation overlay** — verse/chorus/bridge labels on beat timeline.  
  Model: `D:\PROGRAMMER\TYPESCRIPT\ChordMiniApp\SongFormer\src\SongFormer\ckpts\`  
  API: `python_backend/blueprints/songformer/routes.py`

- **Lyrics synchronization** — LRClib timed lyrics overlaid on chord grid.

- **Roman numeral analysis** — harmonic function labels (I, IV, V, etc.) per chord.

- **MIDI export** — download chord progression as MIDI file.

- **Key modulation detection** — highlight when song changes key mid-song.
