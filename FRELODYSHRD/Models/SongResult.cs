using FRELODYAPP.Data;
using FRELODYAPP.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPIs.Areas.Admin.ViewModels
{
    public class SongResult
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public int? SongNumber { get; set; }

        public string? Slug { get; set; }

        public PlayLevel? SongPlayLevel { get; set; }

        public string? WrittenDateRange { get; set; }

        public string? WrittenBy { get; set; }

        public string? History { get; set; }

        public string? CategoryName { get; set; }

        public string? SongBookTitle { get; set; }

        public string? SongBookSlug { get; set; }

        public string? SongBookId { get; set; }

        public string? SongBookDescription { get; set; }

        public string? CategoryId { get; set; }

        public string? CategorySlug { get; set; }

        public bool? IsFavorite { get; set; }
    }

    public class SearchSongResult: SongResult
    {
        public int RelevanceScore { get; set; }
        public string MatchType { get; set; } // "title", "lyrics", "category", "book", "collection", "author", "publisher"
        public string MatchSnippet { get; set; } // Text snippet showing the match
        public string? CollectionTitle { get; set; }
        public string? CollectionCurator { get; set; }

        // For display in UI
        public string DisplayText => $"{Title}" +
            (SongNumber.HasValue ? $" (#{SongNumber})" : "") +
            (!string.IsNullOrEmpty(MatchSnippet) ? $" - {MatchSnippet}" : "");
    }
}
