using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobOnlineAPI.Controllers;

namespace JobOnlineAPI.Services
{
    public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
    {
        private readonly IConfiguration _configuration = configuration;

        public string GenerateJwtToken(UserAdminModel user)
        {
            return GenerateToken(user.Username, user.Role);
        }

        public string GenerateJwtToken(UserModel user)
        {
            return GenerateToken(user.Username, user.Role);
        }

        private string GenerateToken(string username, string role)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 32 bytes long for HMAC-SHA256.");
            }

            var issuer = _configuration["Jwt:Issuer"];
            if (string.IsNullOrEmpty(issuer))
            {
                throw new InvalidOperationException("JWT Issuer is not configured.");
            }

            var audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("JWT Audience is not configured.");
            }

            var key = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}