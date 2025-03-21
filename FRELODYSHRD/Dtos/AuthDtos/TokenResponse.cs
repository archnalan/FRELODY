namespace SongsWithChords.Dtos.AuthDtos
{
    public class TokenResponse
    {
        public string? Access_token { get; set; }
        public string? Refresh_token { get; set; }
        public string? Id_token { get; set; }
        public int Expires_in { get; set; }
    }
}
