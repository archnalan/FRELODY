# FRELODY Documentation — Writer Brief (READ THIS FIRST)

You are an AI writer producing **page content** for the FRELODY documentation site. Each markdown
file in this folder corresponds to exactly one page on the live site (the file's path matches the
URL slug — e.g. `discover/paste-a-link.md` → `/docs/discover/paste-a-link`). Your job is to replace
the placeholder body in each `.md` file with the final, publishable text for that page.

---

## 1. What is FRELODY?

**FRELODY** turns any song into a playable chord chart. You paste a **YouTube, TikTok or video link**;
FRELODY detects the chords, lines them up with the lyrics, and lets you **slow the song down and loop**
the hard parts so you can learn and play along. You can also build and edit your own charts in the
**Compose** editor, organise them into a library, playlists and songbooks, and print or share your work.

### The product avatar — "Alex" (shape everything around this)

Alex is a hobbyist musician who wants to play a song they just heard. The core value loop is:

> **paste a link → get chords + lyrics → slow it down → loop the tricky bars → play it.**

Every page should help Alex move through that loop faster. Lead with the task, not the plumbing. Keep
copy warm, plain and encouraging — never salesy.

### Who uses FRELODY

- **Visitors (anonymous):** browse public pages, preview songs, paste a link and try analysis within the free daily limit.
- **Members (signed in):** the **Compose** editor, personal library, playlists and songbooks, share links, saved profile.
- **Premium (billing active):** unlimited song analysis, **Today's Songs**, longer songs (extended length cap), chord-chart printing.
- **Admins (Owner / Admin / SuperAdmin):** organization dashboard, members & roles, tenants, global products/pricing.

---

## 2. Source of truth

**Two files in this folder are your authoritative inputs. Do not invent features that contradict them.**

1. **[`_WRITER-BRIEF.md`](./_WRITER-BRIEF.md)** — a page-by-page reference compiled from the actual
   product code (`FRELODYUI.Shared/Pages/**`). For every documentation slug it lists the matching Razor
   page(s)/route(s), real UI strings and button labels, related DTOs/services, the **audience tier**, and
   cross-page links. Always open it, find the section for the slug you are writing, and ground your prose
   in those facts. If the brief says *"no direct source — write from requirements only"* for a slug, write
   strictly from the page title and section context; **do not invent product behaviour**.

2. **This README** — defines tone, structure, components and conventions.

---

## 3. Tone and language

- **Plain, friendly, confident.** Short sentences. Active voice. Address the reader directly (*"Paste your link…"*, *"To save your chart, …"*).
- Always call the product **FRELODY**.
- No marketing fluff ("revolutionary", "world-class"). Be concrete and factual.
- British English spelling (analyse, organise, favourite, colour).
- Numbers: spell out one to nine, use digits from 10 upwards.
- Refer to the editor as **Compose**, the analysis area as **Discover**.
- First mention of a paid tier: **Premium**; the free tier is **Starter (free)**.

---

## 4. Page structure

Every page must follow this skeleton (already present in each stub):

```markdown
# <Page Title>

> A one-sentence summary that answers "what will I learn on this page?".

## Overview
2–4 short paragraphs explaining the topic, who it is for and when they would use it.

## Steps        (use only when the page documents a task — otherwise replace with a meaningful H2)
Numbered list of clear, atomic steps. One action per step. Reference real UI labels in **bold**.

## Tips         (optional but encouraged)
Bulleted helpful notes, common pitfalls, accessibility hints.

## Related pages
Bullet list of links to other slugs, formatted as [Title](/docs/<slug>).
```

You may add other `##` sections ("Requirements", "What you'll need", "Limits", "Troubleshooting",
"FAQ") when the topic warrants it. Do **not** add an `H1` other than the title already at the top.

---

## 5. Components available in markdown

The site renders markdown with Markdig + advanced extensions, then injects it as HTML into a Blazor
page. You can therefore use GitHub-flavoured markdown (tables, fenced code, task lists, autolinks,
footnotes), blockquotes (rendered with an accent border — use for the lead summary under the H1),
inline `code` for routes/field names, and **bold** for visible UI labels the user will click.

### Image placeholders

You **cannot** embed Blazor components from inside markdown, so use this raw-HTML placeholder pattern —
the build treats it as raw HTML and an operator later replaces the inner `<div>` with a real `<img>`:

```html
<figure class="img-frame" style="aspect-ratio: 16 / 9;">
  <div class="img-frame-placeholder" role="img" aria-label="Caption goes here">
    <span class="img-frame-caption">Caption goes here</span>
  </div>
</figure>
```

When the screenshot is ready, replace the inner `<div>` with:

```html
<img src="/images/<slug-relative-path>.png" alt="Descriptive alt text" loading="lazy" />
```

**Aspect ratios:** full page screenshot `16 / 9`; form/dialog `4 / 3`; mobile portrait `9 / 16`;
icon/single control `1 / 1`; wide diagram `21 / 9`; **chord chart / fret diagram `4 / 5`**.
At most one image per H2. Keep alt text descriptive (not "screenshot").

### Video embeds (YouTube walkthroughs)

For feature walkthroughs you may embed a YouTube video. Use this raw-HTML pattern — an operator later
fills in the real video ID:

```html
<div class="video-embed">
  <iframe src="https://www.youtube-nocookie.com/embed/VIDEO_ID"
          title="Descriptive video title"
          loading="lazy"
          allow="accelerator; clipboard-write; encrypted-media; picture-in-picture"
          allowfullscreen></iframe>
</div>
<p class="video-embed-caption">Short caption describing the walkthrough.</p>
```

Leave `VIDEO_ID` as a literal placeholder if you don't have a real ID. Use at most one video per page,
near the top of the most relevant H2. The video is automatically hidden in PDF/print output, so never
put information **only** in the video — summarise it in text too.

---

## 6. Cross-linking

Always link to related pages by their slug, e.g.:

```markdown
- [How premium access works](/docs/pricing/how-premium-works)
- [Paste a song link](/docs/discover/paste-a-link)
```

The exhaustive list of valid slugs is the keys in the navigation tree
(`FRELODY.Docs/Services/NavigationDataService.cs`) and equivalently the file names in this folder.
**Never link to a slug that does not exist as a `.md` file in this folder.**

---

## 7. Audience gating (important)

Each page has an **audience** (see the `audience:` line in each stub's `<!-- AI WRITER BRIEF -->`
marker, and `_WRITER-BRIEF.md`):

- **Public** — written for anyone, including signed-out visitors.
- **Member** — the reader is signed in; you can assume they have an account.
- **Premium** — a paid feature; explain the benefit and link to [Plans and billing](/docs/pricing/plans),
  but don't hard-sell.
- **Admin** — back-office; write for Owners/Admins/SuperAdmins managing an organization.

Match the page's voice to its tier. On a gated page you don't need to re-explain how to sign in — link
to [Sign in and sign out](/docs/getting-started/sign-in-and-sign-out) instead.

---

## 8. Per-file workflow

For every `.md` file (other than `_WRITER-BRIEF.md` and this README):

1. Note the file's slug (its path relative to `content/`, without `.md`).
2. Open `_WRITER-BRIEF.md` and locate the section with the same slug.
3. Read its sources, observed behaviours, related models and cross-links.
4. Replace the placeholder body (everything above the `<!-- AI WRITER BRIEF -->` marker) following §4.
5. Use bold UI labels and `code` for routes/field names exactly as they appear in the brief.
6. Add 2–6 **Related pages** links chosen from the brief plus any genuinely-related slug.
7. **Leave the `<!-- AI WRITER BRIEF -->` comment block in place** — it is a silent marker for future regeneration.

---

## 9. What not to do

- **Do not invent features.** If the brief says nothing about a behaviour, do not document it.
- **Do not change page titles, the H1, or the file location** — these are fixed by the navigation tree.
- **Do not add front-matter** (YAML/TOML). Pages are plain markdown.
- **Do not include screenshots inline as base64 or external URLs.** Use the `<figure class="img-frame">` placeholder pattern.
- **Do not put load-bearing information only inside a video** — it is hidden in print.
- **Do not write release notes, changelogs or marketing landing pages.**

---

## 10. Quick reference — brand and entities

| Item | Use exactly |
|---|---|
| Product name | FRELODY |
| What it does (first mention) | FRELODY turns any song into a playable chord chart |
| Editor | Compose (route `/compose`) |
| Analysis area | Discover (route `/discover`) |
| Free tier | Starter (free) |
| Paid tier | Premium (Creator / Studio plans) |
| Payment methods | PayPal, PesaPal |
| Daily-window feature | Today's Songs |

When in doubt: **prefer fewer, true sentences over more, speculative ones.**
