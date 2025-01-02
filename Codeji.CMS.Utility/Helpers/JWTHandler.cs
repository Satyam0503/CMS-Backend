using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace Codeji.CMS.Utility.Helpers
{
	public static class AuthenticationHandler
	{
        public static string HashedPassword(string pass) => BCrypt.Net.BCrypt.HashPassword(pass);
        public static bool VerifyPassword(string toMatch, string forMatch) =>  BCrypt.Net.BCrypt.Verify(toMatch, forMatch);
        public static string GenerateJwtToken(string userId, string companyId,string roleId, List<string> userRole)
        {
            // Retrieve JWT settings from configuration
            var jwtSettings = ConfigurationHelper.config.GetSection("jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryDays = int.Parse(jwtSettings["Expiry"]);

            // Define claims
            var claims = new List<Claim>
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId), // Subject
            new Claim("company_id", companyId),
            // Add multiple roles as separate claims
        
            new Claim("role_id",roleId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique Token ID
            };
            foreach (var role in userRole)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            // Create signing credentials
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            // Create the token
            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expiryDays),
                signingCredentials: credentials
            );

            // Serialize the token
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}

