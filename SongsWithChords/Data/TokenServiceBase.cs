namespace SongsWithChords.Data
{
    public abstract class TokenServiceBase
    {
        public abstract string GetUserIdFromToken(string token);
    }
}