namespace FRELODYAPP.Dtos.SubDtos
{
    public class SongListItemDto : BaseEntityDto
    {
        public int SongNumber { get; set; }
        public string? Title { get; set; }
        public string? WrittenBy { get; set; }
        public string? CategoryName { get; set; }
        public string? AlbumTitle { get; set; }
        public string? ArtistName { get; set; }
        public string? SongBookTitle { get; set; }
        public decimal? Rating { get; set; }
    }
}
