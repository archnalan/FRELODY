namespace FRELODYUI.Shared.Models;

public enum SuggestionSource
{
    Database,
    Recent,
    Extracted,
    YouTube
}

public class StudioSuggestion
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public int? SongNumber { get; set; }
    public string? MatchType { get; set; }
    public string? MatchSnippet { get; set; }
    public double RelevanceScore { get; set; }
    public SuggestionSource Source { get; set; }
}
