using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace GooseWebsite.Api.Modules.Subscriptions.Services;

// Tokens are 32 random bytes, URL-safe; only their SHA-256 hash is ever stored.
internal static class SubscriptionTokens
{
    public static string New() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
