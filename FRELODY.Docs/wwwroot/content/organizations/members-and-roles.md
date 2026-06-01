# Members, roles and ranks

> Learn the organization role hierarchy, the rank each role carries, and the rule for who can manage whom.

## Overview

Every member of an organization holds one role, and each role carries a numeric rank. The ranking decides who can manage whom. You manage members from `/admin/members`, titled "Members — Invite, manage roles, and monitor membership."

The roles, from highest rank to lowest, are: **Owner** (70), **Admin** (60), **Manager** (50), **Editor** (40), **Contributor** (30), **Viewer** (20) and **Guest** (10). The page itself is gated to **Owner**, **Admin** and **Manager** — anyone else sees "Only Owners, Admins, and Managers can manage members."

The core rule is simple: a member can only manage targets they strictly outrank. An Admin (60) can manage a Manager (50) or below, but not another Admin or the Owner. This keeps people from changing the role of a peer or someone above them.

<figure class="img-frame" data-media-slot="organizations-members-and-roles--1" style="aspect-ratio: 16 / 9;">
  <div class="img-frame-placeholder" role="img" aria-label="The Members page listing organization members with their roles">
    <span class="img-frame-caption">The Members page at /admin/members</span>
  </div>
</figure>

## Role hierarchy

| Role | Rank | Typical use |
|---|---|---|
| **Owner** | 70 | Full control of the organization |
| **Admin** | 60 | Manages members and settings |
| **Manager** | 50 | Manages members and projects |
| **Editor** | 40 | Creates and edits content |
| **Contributor** | 30 | Adds content |
| **Viewer** | 20 | Reads content |
| **Guest** | 10 | Limited, light access |

## Who can manage whom

1. Open `/admin/members` (you need the **Owner**, **Admin** or **Manager** role).
2. Find the member you want to manage in the list.
3. You can change a member's role only if your rank is strictly higher than theirs.
4. Use the member actions to add or invite a member, edit roles, resend credentials, or disable and enable a member.

## Tips

- You cannot change the role of a member at your own rank or above — only those strictly below you.
- **Manager** is the lowest role that can open the Members page; Editors and below cannot.
- The deprecated **Moderator** role is folded into **Admin**.

## Related pages

- [Manage members](/docs/administration/manage-members)
- [Organizations overview](/docs/organizations/overview)
- [Organization dashboard](/docs/administration/org-dashboard)

<!-- AI WRITER BRIEF
slug: organizations/members-and-roles
audience: Admin
Write this page following _README.md (tone, page structure, image & video embed patterns)
and the page-by-page facts in _WRITER-BRIEF.md. Replace the Overview placeholder above and
add any task Steps / Tips / Related pages sections that apply. Keep this marker in place.
-->
