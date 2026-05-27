namespace FRELODYUI.Shared.Models
{
    /// <summary>
    /// Lightweight record of the last successfully signed-in user, persisted to localStorage
    /// so the "Continue as &lt;name&gt;" prompt can offer a one-tap return even after the access
    /// token has expired. It is kept under its own key so it survives the session-expiry
    /// cleanup that clears "sessionState".
    /// </summary>
    public class RememberedUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }

        // Stored so the prompt can silently revive the session via the refresh-token endpoint.
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}
