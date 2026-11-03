using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdmissionEngine.API.Models.Entities;
using AdmissionEngine.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AdmissionEngine.API.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var jwtSecret = _configuration["JWT_SECRET"]
                         ?? _configuration["JwtSettings:Secret"]
                         ?? "YourSuperSecretKeyHereThatIsAtLeast32BytesLong!";

            var key = Encoding.UTF8.GetBytes(jwtSecret);

            var claims = new List<Claim>
            {
                new Claim("id", user.Id ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? "student")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(30), // Matches their "30d" expiration
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}