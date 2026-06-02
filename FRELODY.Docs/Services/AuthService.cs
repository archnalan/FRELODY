using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FRELODY.Docs.Models;

namespace FRELODY.Docs.Services;

/// <summary>
/// Authentication shim for the FRELODY docs site.
///
/// The docs site is NOT its own identity provider — it cannot accept credentials
/// or call the FRELODY API's login endpoint. The FRELODY web app is the single
/// source of truth. This service:
///
///  1. On startup, looks for a session passed in the URL fragment
///     (<c>#session=&lt;base64url-json&gt;</c>) by the FRELODY web app, persists it
///     to <c>localStorage</c>, then strips the fragment.
///  2. Restores any previously bridged session from <c>localStorage</c>.
///  3. <see cref="RequestSignIn"/> redirects the browser to the FRELODY web app's
///     <c>/login?returnUrl=&lt;current-docs-url&gt;</c> page. The FRELODY login flow
///     completes there and bounces the browser back to the docs URL with a fresh
///     <c>#session=...</c> fragment.
///
/// The bridged JSON is FRELODY's <c>LoginResponseDto</c> (<c>{ token, refreshToken,
/// tenantId, user }</c>); the JWT itself carries a serialized <c>UserClaimsDto</c>
/// under the <c>"user"</c> claim, from which we derive name, roles and billing tier.
/// </summary>
public class AuthService
{
    // Key used by the docs site's own localStorage cache.
    private const string StorageKey = "frelody.docs.session";

    // Admin-tier roles (from FRELODYSHRD.Constants.UserRoles).
    private static readonly string[] AdminRoles = { "SuperAdmin", "Owner", "Admin" };

    // Billing statuses that count as premium (from FRELODYSHRD.Constants.BillingStatus):
    // PremiumTrial = 2, ActiveRecurring = 3, ActiveLifetime = 4.
    private static readonly string[] PremiumStatusNames = { "PremiumTrial", "ActiveRecurring", "ActiveLifetime" };
    private static readonly int[] PremiumStatusValues = { 2, 3, 4 };

    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;
    private readonly string _webBaseUrl;

    public AuthUser? Current { get; private set; }
    public bool IsAuthenticated => Current is not null;
    public bool IsAdmin => Current?.IsAdmin == true;
    public bool IsPremium => Current?.IsPremium == true;

    /// <summary>
    /// True only for the platform SuperAdmin role. Note this is narrower than <see cref="IsAdmin"/>,
    /// which also includes org Owner/Admin — the docs media manager is SuperAdmin-only to match the API.
    /// </summary>
    public bool IsSuperAdmin =>
        Current?.Roles.Any(r => string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase)) == true;

    public Audience CurrentAudience
    {
        get
        {
            if (Current is null) return Audience.Public;
            if (Current.IsAdmin) return Audience.Admin;
            if (Current.IsPremium) return Audience.Premium;
            return Audience.Member;
        }
    }

    public event Action? OnChanged;

    public AuthService(IJSRuntime js, NavigationManager nav, string webBaseUrl)
    {
        _js = js;
        _nav = nav;
        _webBaseUrl = (webBaseUrl ?? string.Empty).TrimEnd('/');
    }

    /// <summary>
    /// On app startup: pick up a bridged session from the URL fragment if present,
    /// otherwise rehydrate from localStorage. Handles all three hand-off cases
    /// gracefully — no session in the URL, a hand-off carrying no/empty token, and a
    /// malformed or expired token — always landing on a clean URL and a sensible auth
    /// state (a valid session, or anonymous). Best-effort — never throws.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // 1) Did the FRELODY web app just bounce the browser here with a session?
            //    This also strips the #session fragment from the address bar whatever
            //    its contents, so a token is never left visible/shareable and a bad
            //    fragment isn't reprocessed on the next reload.
            var bridged = await TryReadFragmentSessionAsync();

            // 2) Prefer a *valid* bridged token. A missing/empty/expired/garbled one must
            //    NOT clobber an existing good local session (e.g. the user followed a
            //    public "Docs" link that deliberately carried no token) — so only persist
            //    and adopt the bridged token once it actually parses.
            if (!string.IsNullOrWhiteSpace(bridged))
            {
                var bridgedUser = ParseToken(bridged);
                if (bridgedUser is not null)
                {
                    await PersistTokenAsync(bridged); // store only validated tokens
                    Current = bridgedUser;
                    OnChanged?.Invoke();
                    return;
                }

                // Bridged token was invalid/expired — ignore it and fall through to any
                // previously stored session rather than forcing the user anonymous.
                LogDebug("FRELODY.Docs: ignored an invalid or expired bridged session token.");
            }

            // 3) Fall back to a previously stored token. If it has gone stale, drop it so
            //    the user lands cleanly as anonymous instead of in a half-signed state.
            var stored = await GetStoredTokenAsync();
            if (string.IsNullOrWhiteSpace(stored)) return;

            var storedUser = ParseToken(stored);
            if (storedUser is null) { await ClearAsync(); return; }

            Current = storedUser;
            OnChanged?.Invoke();
        }
        catch
        {
            // best-effort restore — never throw on startup
        }
    }

    /// <summary>
    /// Reads <c>#session=&lt;urlsafe-base64-json&gt;</c> from the current URL and extracts
    /// the JWT. If a <c>session=</c> part is present it is stripped from the address bar
    /// regardless of whether it turns out to be valid (so a token is never left visible
    /// and a bad fragment can't replay), while any other fragment — e.g. a deep-link
    /// anchor — is preserved. Returns the JWT, or null if the fragment is absent, carries
    /// no token, or is malformed. Never throws.
    /// </summary>
    private async Task<string?> TryReadFragmentSessionAsync()
    {
        // NavigationManager.Uri carries the fragment in WASM, so we can read it without a
        // JS round-trip (and without eval, which a Content-Security-Policy may block).
        var hashIndex = _nav.Uri.IndexOf('#');
        if (hashIndex < 0) return null;

        var fragment = _nav.Uri.Substring(hashIndex + 1);
        if (string.IsNullOrEmpty(fragment)) return null;

        // Pull out "session=..." and keep every other fragment part so we can rebuild a
        // clean fragment afterwards (supports "#session=...", "#a=b&session=...", and
        // preserves a legitimate "#getting-started" style anchor).
        string? encoded = null;
        var kept = new List<string>();
        foreach (var part in fragment.Split('&'))
        {
            if (encoded is null && part.StartsWith("session=", StringComparison.OrdinalIgnoreCase))
            {
                encoded = part.Substring("session=".Length);
            }
            else if (!string.IsNullOrEmpty(part))
            {
                kept.Add(part);
            }
        }

        // No session hand-off in this fragment — leave the URL untouched (it may be a
        // real doc anchor) and let the caller fall back to a stored session.
        if (encoded is null) return null;

        // A session= WAS present: strip just that part from the address bar now, before
        // any validation, so neither a valid token nor a bad one can linger or replay.
        var baseUri = _nav.Uri.Substring(0, hashIndex);
        var cleanUri = kept.Count > 0 ? $"{baseUri}#{string.Join('&', kept)}" : baseUri;
        try { await _js.InvokeVoidAsync("history.replaceState", null, "", cleanUri); }
        catch { /* address-bar tidy-up is best-effort */ }

        if (encoded.Length == 0) return null; // "#session=" with no payload

        string json;
        try
        {
            json = System.Text.Encoding.UTF8.GetString(Base64UrlDecodeBytes(encoded));
        }
        catch { return null; }

        // The bridged JSON is FRELODY's LoginResponseDto. Extract the JWT — which may be
        // absent (e.g. a public hand-off with a null token), in which case we return null.
        try
        {
            using var doc = JsonDocument.Parse(json);
            return TryGetString(doc.RootElement, "token", "Token", "accessToken", "jwt");
        }
        catch { return null; }
    }

    /// <summary>
    /// Sends the browser to the FRELODY web app's login page with a return URL
    /// pointing back to the current docs page. The FRELODY login flow handles the
    /// credentials (email / Google), then redirects the browser back here with a
    /// fresh <c>#session=...</c>.
    /// </summary>
    public void RequestSignIn()
    {
        if (string.IsNullOrEmpty(_webBaseUrl))
        {
            try { _js.InvokeVoidAsync("console.warn", "FRELODY.Docs: Web:BaseUrl is not configured; cannot redirect to sign in."); } catch { }
            return;
        }

        var returnUrl = Uri.EscapeDataString(_nav.Uri);
        var loginUrl = $"{_webBaseUrl}/login?returnUrl={returnUrl}";
        _nav.NavigateTo(loginUrl, forceLoad: true);
    }

    /// <summary>
    /// Signs the user out. Clearing only the docs-side session is not enough: the
    /// FRELODY web app is the identity provider and keeps its own session in a
    /// different origin's localStorage, so the next <see cref="RequestSignIn"/> would
    /// silently SSO the user straight back. We therefore clear the local session and
    /// then round-trip through the web app's <c>/logout</c> (which clears its session
    /// and bounces back here), giving a true, complete logout.
    /// </summary>
    public async Task LogoutAsync()
    {
        await ClearAsync();

        // No configured web app → docs-local logout is the best we can do.
        if (string.IsNullOrEmpty(_webBaseUrl)) return;

        // Return to the current docs page, minus any fragment (e.g. a stale #session).
        var returnUrl = Uri.EscapeDataString(_nav.Uri.Split('#')[0]);
        var logoutUrl = $"{_webBaseUrl}/logout?returnUrl={returnUrl}";
        _nav.NavigateTo(logoutUrl, forceLoad: true);
    }

    private async Task ClearAsync()
    {
        Current = null;
        try { await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey); } catch { }
        OnChanged?.Invoke();
    }

    private async Task<string?> GetStoredTokenAsync()
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey); }
        catch { return null; }
    }

    private async Task PersistTokenAsync(string token)
    {
        try { await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, token); }
        catch { /* persistence is best-effort; the session still works for this load */ }
    }

    private void LogDebug(string message)
    {
        try { _js.InvokeVoidAsync("console.debug", message); } catch { }
    }

    private static string? TryGetString(JsonElement root, params string[] names)
    {
        foreach (var n in names)
        {
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty(n, out var v) &&
                v.ValueKind == JsonValueKind.String)
            {
                return v.GetString();
            }
        }
        return null;
    }

    private static AuthUser? ParseToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = System.Text.Encoding.UTF8.GetString(Base64UrlDecodeBytes(parts[1]));
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Expiry check.
            if (root.TryGetProperty("exp", out var expEl) && expEl.TryGetInt64(out var exp))
            {
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
                if (expiresAt <= DateTimeOffset.UtcNow) return null;
            }

            var name = ExtractDisplayName(root) ?? "Signed-in user";
            var roles = ExtractRoles(root);
            var isAdmin = roles.Any(r => AdminRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            var isPremium = ExtractIsPremium(root);

            return new AuthUser(name, roles, isAdmin, isPremium, token);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// FRELODY's TokenService serialises a <c>UserClaimsDto</c> JSON blob into a single
    /// <c>"user"</c> claim. Prefer FullName / FirstName+LastName / Email from there;
    /// fall back to standard JWT name claims.
    /// </summary>
    private static string? ExtractDisplayName(JsonElement root)
    {
        if (TryGetUserClaim(root, out var ir))
        {
            var full = TryGetStringCI(ir, "FullName");
            if (!string.IsNullOrWhiteSpace(full)) return full;

            var first = TryGetStringCI(ir, "FirstName");
            var last = TryGetStringCI(ir, "LastName");
            var combined = $"{first} {last}".Trim();
            if (!string.IsNullOrWhiteSpace(combined)) return combined;

            var useName = TryGetStringCI(ir, "UserName");
            if (!string.IsNullOrWhiteSpace(useName)) return useName;

            var email = TryGetStringCI(ir, "Email");
            if (!string.IsNullOrWhiteSpace(email)) return email;
        }

        return TryGetString(root,
            "name", "unique_name", "preferred_username",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
            "email", "sub");
    }

    /// <summary>
    /// Premium tier is determined from the serialized "user" claim's BillingStatus
    /// (mirrors GlobalAuthStateProvider.IsPremiumUser). BillingStatus may serialise as
    /// an enum number (2/3/4) or its string name.
    /// </summary>
    private static bool ExtractIsPremium(JsonElement root)
    {
        if (!TryGetUserClaim(root, out var ir)) return false;
        if (ir.ValueKind != JsonValueKind.Object) return false;

        foreach (var p in ir.EnumerateObject())
        {
            if (!string.Equals(p.Name, "BillingStatus", StringComparison.OrdinalIgnoreCase)) continue;
            switch (p.Value.ValueKind)
            {
                case JsonValueKind.String:
                    var s = p.Value.GetString();
                    return s is not null && PremiumStatusNames.Contains(s, StringComparer.OrdinalIgnoreCase);
                case JsonValueKind.Number:
                    return p.Value.TryGetInt32(out var n) && PremiumStatusValues.Contains(n);
            }
        }
        return false;
    }

    /// <summary>
    /// The "user" claim is itself a JSON string. Parse it and hand back the inner object.
    /// </summary>
    private static bool TryGetUserClaim(JsonElement root, out JsonElement inner)
    {
        inner = default;
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("user", out var userEl) &&
            userEl.ValueKind == JsonValueKind.String)
        {
            var raw = userEl.GetString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    using var innerDoc = JsonDocument.Parse(raw);
                    inner = innerDoc.RootElement.Clone();
                    return true;
                }
                catch { /* malformed inner JSON */ }
            }
        }
        return false;
    }

    private static string? TryGetStringCI(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) &&
                p.Value.ValueKind == JsonValueKind.String)
            {
                return p.Value.GetString();
            }
        }
        return null;
    }

    private static IReadOnlyList<string> ExtractRoles(JsonElement root)
    {
        var roles = new List<string>();

        // 1) Standard JWT role claims (TokenService emits one ClaimTypes.Role per role).
        string[] roleClaims =
        {
            "role",
            "roles",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        foreach (var key in roleClaims)
        {
            if (!root.TryGetProperty(key, out var v)) continue;
            if (v.ValueKind == JsonValueKind.String)
            {
                roles.Add(v.GetString()!);
            }
            else if (v.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in v.EnumerateArray())
                {
                    if (e.ValueKind == JsonValueKind.String) roles.Add(e.GetString()!);
                }
            }
        }

        // 2) The serialized user claim's Roles list (authoritative).
        if (TryGetUserClaim(root, out var ir) && ir.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in ir.EnumerateObject())
            {
                if (string.Equals(p.Name, "Roles", StringComparison.OrdinalIgnoreCase) &&
                    p.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in p.Value.EnumerateArray())
                    {
                        if (e.ValueKind == JsonValueKind.String) roles.Add(e.GetString()!);
                    }
                }
            }
        }

        return roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static byte[] Base64UrlDecodeBytes(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}

public sealed record AuthUser(
    string Name,
    IReadOnlyList<string> Roles,
    bool IsAdmin,
    bool IsPremium,
    string Token);
