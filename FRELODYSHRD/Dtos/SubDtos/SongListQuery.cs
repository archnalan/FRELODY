namespace FRELODYAPP.Dtos.SubDtos
{
    public class SongListQuery
    {
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = 10;
        public string? Search { get; set; }
        public SongListSortField? SortBy { get; set; }
        public SortDirection SortDir { get; set; } = SortDirection.Asc;
    }

    public enum SongListSortField
    {
        SongNumber = 0,
        Title = 1,
        Rating = 2,
        Category = 3,
        Artist = 4,
    }

    public enum SortDirection
    {
        Asc = 0,
        Desc = 1,
    }
}
