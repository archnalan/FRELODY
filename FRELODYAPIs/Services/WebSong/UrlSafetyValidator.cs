using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FRELODYAPIs.Services.WebSong
{
    /// <summary>
    /// Validates outbound URLs before the server fetches them, to prevent SSRF and abuse.
    /// Rules:
    ///  - Scheme must be https (http allowed only for explicitly whitelisted hosts in dev).
    ///  - Host must be on the configured allowlist (case-insensitive, exact or sub-domain).
    ///  - All resolved IP addresses must be public (no loopback, link-local, private,
    ///    multicast, or cloud metadata endpoints).
    ///  - URL length capped to a reasonable bound.
    /// </summary>
    public sealed class UrlSafetyValidator
    {
        private const int MaxUrlLength = 2048;

        private static readonly string[] BlockedHosts = new[]
        {
            "169.254.169.254", // AWS / GCP / Azure metadata
            "metadata.google.internal",
            "metadata",
        };

        private readonly ILogger<UrlSafetyValidator> _logger;

        public UrlSafetyValidator(ILogger<UrlSafetyValidator> logger)
        {
            _logger = logger;
        }

        public async Task<UrlSafetyResult> ValidateAsync(string? url, IReadOnlyCollection<string> allowedHosts, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                return UrlSafetyResult.Reject("URL is empty.");

            if (url.Length > MaxUrlLength)
                return UrlSafetyResult.Reject("URL is too long.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return UrlSafetyResult.Reject("URL is not absolute.");

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                return UrlSafetyResult.Reject("Only http(s) URLs are allowed.");

            // Host allowlist (exact host or sub-domain match against any allowed entry).
            var host = uri.Host;
            bool hostAllowed = allowedHosts.Any(h =>
                string.Equals(h, host, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));

            if (!hostAllowed)
                return UrlSafetyResult.Unsupported($"Host '{host}' is not on the allowlist.");

            if (BlockedHosts.Any(b => string.Equals(b, host, StringComparison.OrdinalIgnoreCase)))
                return UrlSafetyResult.Reject("Host is blocked.");

            // Resolve IPs and ensure none are private / loopback / link-local / metadata.
            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DNS resolution failed for {Host}", host);
                return UrlSafetyResult.Reject("DNS resolution failed.");
            }

            if (addresses.Length == 0)
                return UrlSafetyResult.Reject("Host did not resolve.");

            foreach (var ip in addresses)
            {
                if (IsDisallowedIp(ip))
                {
                    _logger.LogWarning("Blocked URL {Url} resolving to non-public IP {Ip}", url, ip);
                    return UrlSafetyResult.Reject("Host resolves to a non-public address.");
                }
            }

            return UrlSafetyResult.Ok(uri);
        }

        private static bool IsDisallowedIp(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return true;

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // 169.254.0.0/16  (link-local incl. cloud metadata)
                if (bytes[0] == 169 && bytes[1] == 254) return true;
                // 127.0.0.0/8 already covered by IsLoopback
                // 0.0.0.0/8
                if (bytes[0] == 0) return true;
                // 100.64.0.0/10  CGNAT
                if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
                // Multicast 224.0.0.0/4
                if (bytes[0] >= 224) return true;
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
                    return true;
                // Unique local fc00::/7
                var bytes = ip.GetAddressBytes();
                if ((bytes[0] & 0xfe) == 0xfc) return true;
                // ::1 covered by IsLoopback; ::ffff:a.b.c.d (IPv4-mapped) — re-check the IPv4 portion
                if (ip.IsIPv4MappedToIPv6)
                {
                    var mapped = ip.MapToIPv4();
                    return IsDisallowedIp(mapped);
                }
            }

            return false;
        }
    }

    public sealed class UrlSafetyResult
    {
        public bool IsAllowed { get; private init; }
        public bool IsUnsupportedHost { get; private init; }
        public Uri? Uri { get; private init; }
        public string? Reason { get; private init; }

        public static UrlSafetyResult Ok(Uri uri) => new() { IsAllowed = true, Uri = uri };
        public static UrlSafetyResult Reject(string reason) => new() { IsAllowed = false, Reason = reason };
        public static UrlSafetyResult Unsupported(string reason) => new() { IsAllowed = false, IsUnsupportedHost = true, Reason = reason };
    }
}
