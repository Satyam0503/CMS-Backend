using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services.CareerPortal;

internal static class CareerTokenProtector
{
    public static string CreateUnsubscribeToken(string subscriberId)
    {
        var expires = DateTimeOffset.UtcNow.AddYears(2).ToUnixTimeSeconds();
        var payload = $"{subscriberId}.{expires}";
        return $"{Encode(Encoding.UTF8.GetBytes(payload))}.{Sign(payload)}";
    }

    public static string? ReadUnsubscribeToken(string token)
    {
        var separator = token.LastIndexOf('.');
        if (separator <= 0) return null;
        try
        {
            var payload = Encoding.UTF8.GetString(Decode(token[..separator]));
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(Sign(payload)),
                    Encoding.UTF8.GetBytes(token[(separator + 1)..]))) return null;
            var parts = payload.Split('.');
            return parts.Length == 2 && long.TryParse(parts[1], out var expires) &&
                   DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expires ? parts[0] : null;
        }
        catch { return null; }
    }

    private static string Sign(string payload)
    {
        var key = Encoding.UTF8.GetBytes(ConfigManager.Jwt.SecretKey);
        return Encode(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)));
    }
    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}
