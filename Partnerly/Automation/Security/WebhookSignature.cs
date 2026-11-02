using System.Security.Cryptography;
using System.Text;

namespace Partnerly.Automation.Security
{
    public static class WebhookSignature
    {
        public const string HeaderName = "X-Partnerly-Signature";

        public static string Compute(string secret, string body)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body ?? string.Empty));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool Verify(string secret, string body, string? signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            var expected = Compute(secret, body);
            var actual = signature.Trim().ToLowerInvariant();
            if (actual.StartsWith("sha256=", StringComparison.Ordinal))
            {
                actual = actual["sha256=".Length..];
            }

            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            var actualBytes = Encoding.UTF8.GetBytes(actual);
            return expectedBytes.Length == actualBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
    }
}
