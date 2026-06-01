# Manage members

> Invite people, change roles, resend credentials and disable or enable members of your organization.

## Overview

You manage your team from `/admin/members`, titled "Members — Invite, manage roles, and monitor membership." The page is gated to **Owner**, **Admin** and **Manager**; anyone else sees "Only Owners, Admins, and Managers can manage members."

From here you can add a member two ways: **invite** an existing FRELODY user, or **create** an account for someone new. You can also edit a member's role, resend their credentials, and disable or enable their access.

Changing a role opens a confirm dialog ("Change member roles", with a **Save changes** action) so the change is deliberate. Remember the management rule: you can only act on members you strictly outrank — see Members, roles and ranks for the full hierarchy.

<figure class="img-frame" data-media-slot="administration-manage-members--1" style="aspect-ratio: 16 / 9;">
  <div class="img-frame-placeholder" role="img" aria-label="The Members page with invite, role-edit and enable controls">
    <span class="img-frame-caption">The Members page at /admin/members</span>
  </div>
</figure>

## Steps

1. Open `/admin/members` (you need the **Owner**, **Admin** or **Manager** role).
2. To add someone, choose to **invite** an existing user or **create** a new account.
3. To change a role, edit the member and confirm with **Save changes**.
4. Use **resend credentials** to send sign-in details again.
5. Use **disable** or **enable** to suspend or restore a member's access.

## Tips

- You can only manage members ranked below you — not peers or anyone above you.
- Use **invite** when the person already has a FRELODY account; use **create** to set one up for them.

## Related pages

- [Members, roles and ranks](/docs/organizations/members-and-roles)
- [Organization dashboard](/docs/administration/org-dashboard)

<!-- AI WRITER BRIEF
slug: administration/manage-members
audience: Admin
Write this page following _README.md (tone, page structure, image & video embed patterns)
and the page-by-page facts in _WRITER-BRIEF.md. Replace the Overview placeholder above and
add any task Steps / Tips / Related pages sections that apply. Keep this marker in place.
-->
