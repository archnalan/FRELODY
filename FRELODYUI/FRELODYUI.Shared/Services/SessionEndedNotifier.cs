using System;
using System.Linq;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    /// <summary>
    /// Scoped signal raised by <see cref="AuthHeaderHandler"/> when a token refresh
    /// hard-fails — i.e. the session is no longer recoverable (refresh token revoked,
    /// expired beyond refresh, or the access token was tampered with so the server
    /// rejects it and the refresh too). A single <c>SessionGuard</c> (mounted in
    /// Routes.razor) subscribes and performs the clear-state + toast + redirect-to-login.
    ///
    /// This decouples the HTTP pipeline (which has no NavigationManager / renderer
    /// context of its own) from the UI reaction, while staying inside the same DI
    /// scope as the rest of the circuit.
    /// </summary>
    public class SessionEndedNotifier
    {
        /// <summary>Raised with an optional human-readable reason for the sign-out.</summary>
        public event Func<string?, Task>? OnSessionEnded;

        public async Task NotifySessionEndedAsync(string? reason = null)
        {
            var handler = OnSessionEnded;
            if (handler is null) return;

            foreach (var subscriber in handler.GetInvocationList().Cast<Func<string?, Task>>())
            {
                // Best-effort: one failing subscriber must not block the others.
                try { await subscriber(reason); } catch { /* swallow */ }
            }
        }
    }
}
