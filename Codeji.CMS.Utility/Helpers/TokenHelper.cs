using System.Security.Cryptography;
using System.Text;

namespace Codeji.CMS.Utility.Helpers;

public class TokenHelper
{
    public static string GenerateToken(int byteLength = 64)
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(tokenBytes);
    }

    public static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }
}