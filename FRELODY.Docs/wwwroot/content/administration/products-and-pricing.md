# Products and pricing (SuperAdmin)

> Edit the plans and features shown on the public pricing page — a SuperAdmin-only tool.

## Overview

This screen lets platform administrators edit the plans that appear on the public pricing page. Open `/admin/products`, titled "Plans & pricing — Edit the plans shown on the public pricing page. Changes are live immediately."

Access is gated to the **SuperAdmin** platform role — this is not the same as an organization Owner or Admin. If you lack it you will see "Only platform administrators can manage plans and pricing." The page does not require an organization.

Each plan's features are chosen from the same feature catalog that drives the public pricing cards, so what you toggle here is exactly what visitors see. The catalog includes features such as **AutoChordDetection**, **SlowDownPractice**, **SectionLooping**, **ChordTimeline**, **PlaylistSaving**, **SongSharing**, **PdfExport**, **UnlimitedAnalyses**, **ExtendedSongLength**, **PrioritySupport** and **SharedTeamLibrary**.

<figure class="img-frame" style="aspect-ratio: 16 / 9;">
  <div class="img-frame-placeholder" role="img" aria-label="The Plans and pricing editor listing plans with their feature toggles">
    <span class="img-frame-caption">The Plans &amp; pricing editor at /admin/products</span>
  </div>
</figure>

## Steps

1. Open `/admin/products` (you need the **SuperAdmin** platform role).
2. Select **Add plan** to create a plan, or choose an existing plan to edit.
3. Set the plan's name, price, currency and billing period.
4. Toggle the **features** included in the plan from the catalog.
5. Save your changes — they go live on the public pricing page immediately.

## Tips

- Because changes are live straight away, double-check price and currency before saving.
- The features here are the single source of truth shared with the public pricing page.

## Related pages

- [Plans and billing](/docs/pricing/plans)
- [How premium access works](/docs/pricing/how-premium-works)
- [Organization dashboard](/docs/administration/org-dashboard)

<!-- AI WRITER BRIEF
slug: administration/products-and-pricing
audience: Admin
Write this page following _README.md (tone, page structure, image & video embed patterns)
and the page-by-page facts in _WRITER-BRIEF.md. Replace the Overview placeholder above and
add any task Steps / Tips / Related pages sections that apply. Keep this marker in place.
-->
