using System.Security.Cryptography;
using System.Text;

namespace Partnerly.Computer
{
    // The pairing token of a computer: "pc.{connectionId}.{secret}". Only a hash of the
    // secret is stored (inside the encrypted payload of the connection), so a leaked
    // database does not give anyone a way to impersonate the computer.
    public static class ComputerToken
    {
        public const string Prefix = "pc";

        public static (string Token, string Hash) Create(Guid connectionId)
        {
            var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            return ($"{Prefix}.{connectionId:N}.{secret}", Hash(secret));
        }

        public static bool TryParse(string? token, out Guid connectionId, out string secret)
        {
            connectionId = Guid.Empty;
            secret = string.Empty;
            var parts = (token ?? string.Empty).Trim().Split('.');
            if (parts.Length != 3 || parts[0] != Prefix || parts[2].Length < 16 || !Guid.TryParseExact(parts[1], "N", out connectionId))
            {
                return false;
            }

            secret = parts[2];
            return true;
        }

        public static string Hash(string secret) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();

        public static bool Matches(string secret, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Hash(secret)),
                Encoding.UTF8.GetBytes(storedHash));
        }
    }
}
