using FRELODYAPP.Dtos.AuthDtos;

namespace FRELODYUI.Shared.Services
{
    /// <summary>
    /// Bridges the auth session into the <see cref="System.Net.Http.IHttpClientFactory"/>
    /// handler scope on the Blazor <b>Server</b> render path.
    ///
    /// Refit/HttpClientFactory build the message-handler chain in their own DI scope,
    /// not the Blazor circuit's scope, so an <c>AuthHeaderHandler</c> running there gets
    /// an <c>IJSRuntime</c> that is NOT connected to the browser — its localStorage read
    /// fails and the bearer token is dropped, making the API see an authenticated user as
    /// anonymous. Implementations resolve the <i>circuit's</i> services (via an AsyncLocal
    /// bridge) so the real session can be read.
    ///
    /// Only registered in the server host. WASM/MAUI leave it unregistered (their single
    /// DI scope can always read localStorage), so the handler simply skips this fallback.
    /// </summary>
    public interface ICircuitTokenFallback
    {
        /// <summary>The current circuit's session, or null when off-circuit / unavailable.</summary>
        Task<LoginResponseDto?> GetSessionAsync();
    }
}
