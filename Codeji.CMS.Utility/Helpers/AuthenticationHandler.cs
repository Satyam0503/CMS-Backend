using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Codeji.CMS.Utility.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Utility.Helpers
{
    public static class AuthenticationHandler
    {
        public static string HashedPassword(string pass) => BCrypt.Net.BCrypt.HashPassword(pass);
        public static bool VerifyPassword(string toMatch, string forMatch) => BCrypt.Net.BCrypt.Verify(toMatch, forMatch);
        public static string GenerateJwtToken(string userId, string companyId, string roleId, List<string> userRole)
        {
            byte[] key = Encoding.UTF8.GetBytes(ConfigManager.Jwt.SecretKey);
            int expiryMinutes = ConfigManager.Jwt.Expiry;

            // Define claims
            List<Claim> claims = new List<Claim>
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId), // Subject
            new Claim("user_id", userId),
            new Claim("company_id", companyId),
            // Add multiple roles as separate claims

            new Claim("role_id",roleId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique Token ID
            };
            foreach (string role in userRole)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            // Create signing credentials
            SigningCredentials credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            // Create the token
            JwtSecurityToken tokenDescriptor = new JwtSecurityToken(
                issuer: ConfigManager.AppSettings.APIUrl,
                audience: ConfigManager.AppSettings.AppUrl,
                claims: claims,
                // expires: DateTime.UtcNow.AddDays(expiryDays),
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            // Serialize the token
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}

