namespace FRELODYAPP.Data
{
    public abstract class TokenServiceBase
    {
        public abstract string GetUserIdFromToken(string token);
    }
}