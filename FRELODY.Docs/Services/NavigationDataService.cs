using FRELODY.Docs.Models;

namespace FRELODY.Docs.Services;

/// <summary>
/// Single source of truth for the FRELODY documentation navigation.
/// Each leaf <see cref="NavItem.Slug"/> maps to a markdown file under wwwroot/content/{Slug}.md
/// and to the route /docs/{Slug}. Audience tiers gate visibility:
/// Public (everyone) → Member (signed in) → Premium (billing active) → Admin.
/// </summary>
public class NavigationDataService
{
    public IReadOnlyList<NavItem> Sections { get; }

    public NavigationDataService()
    {
        Sections = Build();
    }

    private static List<NavItem> Build() => new()
    {
        new NavItem
        {
            Title = "Getting started",
            Icon = "rocket",
            Children =
            {
                new NavItem { Title = "Welcome to FRELODY", Slug = "getting-started/welcome" },
                new NavItem { Title = "How FRELODY works", Slug = "getting-started/how-frelody-works" },
                new NavItem { Title = "How to use this guide", Slug = "getting-started/how-to-use-this-guide" },
                new NavItem { Title = "Create an account", Slug = "getting-started/create-an-account" },
                new NavItem { Title = "Sign in and sign out", Slug = "getting-started/sign-in-and-sign-out" },
                new NavItem { Title = "Light and dark themes", Slug = "getting-started/themes-and-display" }
            }
        },
        new NavItem
        {
            Title = "Discover & song analysis",
            Icon = "search",
            Children =
            {
                new NavItem { Title = "What Discover does", Slug = "discover/overview" },
                new NavItem { Title = "Paste a song link", Slug = "discover/paste-a-link" },
                new NavItem { Title = "Analyse a YouTube song", Slug = "discover/youtube-analysis", Audience = Audience.Member },
                new NavItem { Title = "Analyse a TikTok video", Slug = "discover/tiktok-analysis", Audience = Audience.Member },
                new NavItem { Title = "Chord playback and timeline", Slug = "discover/chord-playback", Audience = Audience.Member },
                new NavItem { Title = "Free daily limits and song length", Slug = "discover/free-limits" },
                new NavItem { Title = "Today's Songs", Slug = "discover/todays-songs", Audience = Audience.Premium },
                new NavItem { Title = "Unlimited analysis (Premium)", Slug = "discover/unlimited-analysis", Audience = Audience.Premium }
            }
        },
        new NavItem
        {
            Title = "Compose — the song editor",
            Icon = "edit",
            Children =
            {
                new NavItem { Title = "The song editor (Compose)", Slug = "compose/overview", Audience = Audience.Member },
                new NavItem { Title = "Import a song", Slug = "compose/import-a-song", Audience = Audience.Member },
                new NavItem { Title = "Editing sections and rows", Slug = "compose/editing-sections", Audience = Audience.Member },
                new NavItem { Title = "Adding chords and lyrics", Slug = "compose/adding-chords-and-lyrics", Audience = Audience.Member },
                new NavItem { Title = "Saving and session recovery", Slug = "compose/saving-and-recovery", Audience = Audience.Member },
                new NavItem
                {
                    Title = "Chords and chord charts",
                    Children =
                    {
                        new NavItem { Title = "Draw a chord (chord-draw)", Slug = "compose/chord-draw" },
                        new NavItem { Title = "Create and edit chord charts", Slug = "compose/chord-charts", Audience = Audience.Member },
                        new NavItem { Title = "Viewing a chord chart", Slug = "compose/viewing-chord-charts" },
                        new NavItem { Title = "The chord reference", Slug = "compose/chord-reference" }
                    }
                }
            }
        },
        new NavItem
        {
            Title = "Library, playlists & songbooks",
            Icon = "book",
            Children =
            {
                new NavItem { Title = "Your songs library", Slug = "library/songs-library", Audience = Audience.Member },
                new NavItem { Title = "Viewing and playing a song", Slug = "library/viewing-a-song" },
                new NavItem { Title = "Previewing a song without an account", Slug = "library/song-preview" },
                new NavItem { Title = "Playlists and folders", Slug = "library/playlists", Audience = Audience.Member },
                new NavItem { Title = "Songbooks and categories", Slug = "library/songbooks", Audience = Audience.Member }
            }
        },
        new NavItem
        {
            Title = "Pricing & premium",
            Icon = "gem",
            Children =
            {
                new NavItem { Title = "Plans and billing", Slug = "pricing/plans" },
                new NavItem { Title = "How premium access works", Slug = "pricing/how-premium-works" },
                new NavItem { Title = "Pay with PayPal", Slug = "pricing/paypal-checkout", Audience = Audience.Member },
                new NavItem { Title = "Pay with PesaPal", Slug = "pricing/pesapal-checkout", Audience = Audience.Member },
                new NavItem { Title = "Managing your subscription", Slug = "pricing/managing-your-subscription", Audience = Audience.Premium }
            }
        },
        new NavItem
        {
            Title = "Printing & export",
            Icon = "printer",
            Children =
            {
                new NavItem { Title = "Print chord charts", Slug = "printing/print-chord-charts", Audience = Audience.Premium },
                new NavItem { Title = "Export songs as PDF", Slug = "printing/export-pdf", Audience = Audience.Premium },
                new NavItem { Title = "Download this guide", Slug = "printing/download-this-guide" }
            }
        },
        new NavItem
        {
            Title = "Sharing",
            Icon = "share",
            Children =
            {
                new NavItem { Title = "Share links", Slug = "sharing/share-links", Audience = Audience.Member },
                new NavItem { Title = "Public song and playlist pages", Slug = "sharing/public-landings" },
                new NavItem { Title = "Social cards (Open Graph)", Slug = "sharing/social-cards" }
            }
        },
        new NavItem
        {
            Title = "Account & profile",
            Icon = "user",
            Children =
            {
                new NavItem { Title = "Your profile", Slug = "account/your-profile", Audience = Audience.Member },
                new NavItem { Title = "Library and recovery", Slug = "account/library-and-recovery", Audience = Audience.Member },
                new NavItem { Title = "Settings", Slug = "account/settings", Audience = Audience.Member },
                new NavItem { Title = "Password and security", Slug = "account/password-and-security", Audience = Audience.Member }
            }
        },
        new NavItem
        {
            Title = "Organizations & teams",
            Icon = "users",
            Children =
            {
                new NavItem { Title = "Organizations overview", Slug = "organizations/overview", Audience = Audience.Member },
                new NavItem { Title = "Create an organization", Slug = "organizations/create-an-organization", Audience = Audience.Member },
                new NavItem { Title = "Join an organization", Slug = "organizations/join-an-organization", Audience = Audience.Member },
                new NavItem { Title = "Members, roles and ranks", Slug = "organizations/members-and-roles", Audience = Audience.Admin },
                new NavItem { Title = "Projects", Slug = "organizations/projects", Audience = Audience.Admin }
            }
        },
        new NavItem
        {
            Title = "Administration",
            Icon = "shield",
            Children =
            {
                new NavItem { Title = "Organization dashboard", Slug = "administration/org-dashboard", Audience = Audience.Admin },
                new NavItem { Title = "Manage members", Slug = "administration/manage-members", Audience = Audience.Admin },
                new NavItem { Title = "Tenants", Slug = "administration/tenants", Audience = Audience.Admin },
                new NavItem { Title = "Products and pricing (SuperAdmin)", Slug = "administration/products-and-pricing", Audience = Audience.Admin }
            }
        },
        new NavItem
        {
            Title = "Help",
            Icon = "help",
            Children =
            {
                new NavItem { Title = "Frequently asked questions", Slug = "help/faq" },
                new NavItem { Title = "Glossary", Slug = "help/glossary" },
                new NavItem { Title = "Get support", Slug = "help/get-support" }
            }
        }
    };

    /// <summary>
    /// Walk the tree and return the trail of <see cref="NavItem"/>s leading to a slug.
    /// First item is the top-level section; last item is the matching page.
    /// </summary>
    public List<NavItem> GetBreadcrumbTrail(string slug)
    {
        var trail = new List<NavItem>();
        foreach (var section in Sections)
        {
            if (TryFind(section, slug, trail))
            {
                return trail;
            }
            trail.Clear();
        }
        return trail;
    }

    private static bool TryFind(NavItem node, string slug, List<NavItem> trail)
    {
        trail.Add(node);
        if (string.Equals(node.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        foreach (var child in node.Children)
        {
            if (TryFind(child, slug, trail))
            {
                return true;
            }
        }
        trail.RemoveAt(trail.Count - 1);
        return false;
    }

    /// <summary>
    /// Find adjacent (previous / next) leaf pages relative to the given slug, in document order.
    /// </summary>
    public (NavItem? prev, NavItem? next) GetAdjacent(string slug)
    {
        var leaves = new List<NavItem>();
        foreach (var s in Sections)
        {
            CollectLeaves(s, leaves);
        }

        var index = leaves.FindIndex(l => string.Equals(l.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return (null, null);
        var prev = index > 0 ? leaves[index - 1] : null;
        var next = index < leaves.Count - 1 ? leaves[index + 1] : null;
        return (prev, next);
    }

    private static void CollectLeaves(NavItem node, List<NavItem> leaves)
    {
        if (!node.HasChildren && !string.IsNullOrEmpty(node.Slug))
        {
            leaves.Add(node);
        }
        foreach (var child in node.Children)
        {
            CollectLeaves(child, leaves);
        }
    }

    /// <summary>
    /// Flat search across page titles and descriptions.
    /// </summary>
    public IEnumerable<NavItem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) yield break;
        var q = query.Trim();
        foreach (var section in Sections)
        {
            foreach (var match in SearchNode(section, q))
            {
                yield return match;
            }
        }
    }

    private static IEnumerable<NavItem> SearchNode(NavItem node, string q)
    {
        if (!node.HasChildren && !string.IsNullOrEmpty(node.Slug))
        {
            if (node.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (node.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                yield return node;
            }
        }
        foreach (var child in node.Children)
        {
            foreach (var m in SearchNode(child, q))
            {
                yield return m;
            }
        }
    }

    /// <summary>
    /// Find a leaf NavItem by its slug, or null if not found.
    /// </summary>
    public NavItem? FindBySlug(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return null;
        foreach (var s in Sections)
        {
            var hit = FindNode(s, slug);
            if (hit is not null) return hit;
        }
        return null;
    }

    private static NavItem? FindNode(NavItem node, string slug)
    {
        if (string.Equals(node.Slug, slug, StringComparison.OrdinalIgnoreCase)) return node;
        foreach (var child in node.Children)
        {
            var hit = FindNode(child, slug);
            if (hit is not null) return hit;
        }
        return null;
    }

    /// <summary>
    /// Resolve the audience required for a given slug. Returns Public if the slug is unknown.
    /// </summary>
    public Audience GetAudienceForSlug(string slug)
        => FindBySlug(slug)?.Audience ?? Audience.Public;
}
