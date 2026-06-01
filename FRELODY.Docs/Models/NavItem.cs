namespace FRELODY.Docs.Models;

public class NavItem
{
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Route slug. For leaf pages this maps to /docs/{Slug} and to a markdown file at content/{Slug}.md.
    /// For groups without a landing page, leave empty.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? Description { get; set; }

    public List<NavItem> Children { get; set; } = new();

    public bool HasChildren => Children.Count > 0;

    public bool IsGroup => HasChildren;

    /// <summary>
    /// Audience required to see this item. For groups, the effective audience
    /// is the least-restrictive child via <see cref="EffectiveAudience"/>.
    /// </summary>
    public Audience Audience { get; set; } = Audience.Public;

    /// <summary>
    /// For leaves: returns <see cref="Audience"/>.
    /// For groups: returns the LEAST restrictive audience among children
    /// (so a group with at least one Public child stays Public; the group is
    /// hidden only when every child is gated above the current viewer's tier).
    /// </summary>
    public Audience EffectiveAudience
    {
        get
        {
            if (!HasChildren) return Audience;
            var min = Audience.Admin;
            foreach (var c in Children)
            {
                var ca = c.EffectiveAudience;
                if (ca < min) min = ca;
                if (min == Audience.Public) break;
            }
            return min;
        }
    }
}
