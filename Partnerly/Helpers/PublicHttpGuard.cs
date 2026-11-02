using System.Net;
using System.Net.Sockets;

namespace Partnerly.Helpers
{
    public static class PublicHttpGuard
    {
        public static bool IsAllowed(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && IsAllowed(uri);
        }

        public static bool IsAllowed(Uri uri)
        {
            if (!IsHttp(uri) || uri.IsLoopback || IsBlockedHost(uri))
            {
                return false;
            }

            var host = Host(uri);
            if (!IPAddress.TryParse(host, out var address))
            {
                return true;
            }

            return !IsBlockedAddress(address);
        }

        public static async Task<bool> IsAllowedAsync(string? url, CancellationToken cancellationToken = default)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                   && await IsAllowedAsync(uri, cancellationToken);
        }

        public static async Task<bool> IsAllowedAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsHttp(uri) || uri.IsLoopback || IsBlockedHost(uri))
            {
                return false;
            }

            var host = Host(uri);
            IPAddress[] addresses;
            try
            {
                if (IPAddress.TryParse(host, out var parsed))
                {
                    addresses = new[] { parsed };
                }
                else
                {
                    addresses = await Dns.GetHostAddressesAsync(host);
                }
            }
            catch
            {
                return false;
            }

            return addresses.Length > 0 && addresses.All(address => !IsBlockedAddress(address));
        }

        private static bool IsHttp(Uri uri) =>
            uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

        private static string Host(Uri uri) =>
            (uri.IdnHost ?? uri.Host ?? string.Empty).Trim().Trim('[', ']');

        private static bool IsBlockedHost(Uri uri)
        {
            var host = Host(uri);
            if (host.Length == 0)
            {
                return true;
            }

            return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                   || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
                   || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
                   || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
                   || host.Equals("metadata", StringComparison.OrdinalIgnoreCase)
                   || host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBlockedAddress(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            if (IPAddress.IsLoopback(ip) || Equals(ip, IPAddress.Any) || Equals(ip, IPAddress.IPv6Any))
            {
                return true;
            }

            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
            {
                return true;
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var bytes = ip.GetAddressBytes();
                return (bytes[0] & 0xFE) == 0xFC;
            }

            var v4 = ip.GetAddressBytes();
            if (v4[0] == 0 || v4[0] == 10 || v4[0] == 127)
            {
                return true;
            }

            if (v4[0] == 169 && v4[1] == 254)
            {
                return true;
            }

            if (v4[0] == 172 && v4[1] >= 16 && v4[1] <= 31)
            {
                return true;
            }

            if (v4[0] == 192 && v4[1] == 168)
            {
                return true;
            }

            if (v4[0] == 100 && v4[1] >= 64 && v4[1] <= 127)
            {
                return true;
            }

            return v4[0] >= 224;
        }
    }
}
