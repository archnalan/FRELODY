namespace FRELODY.Docs.Models;

public enum MediaKind
{
    Image,
    Video
}

/// <summary>
/// A single curated media slot. Maps 1:1 to a <c>data-media-slot</c> placeholder in the docs
/// markdown. The <see cref="Context"/> is the human "what goes here" hint shown to the SuperAdmin
/// on the media manager page (harvested from the placeholder's aria-label / iframe title).
/// </summary>
public sealed record MediaSlot(
    string Key,
    string PageSlug,
    MediaKind Kind,
    string AspectRatio,
    string Context);
