using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Opilochka.Data;
using Opilochka.Data.Users;
using System.Security.Claims;
using System.Text;

namespace Opilochka.Core.Auth
{
    public class JwtProvider
    {
        private readonly JwtOptions _options;

        public JwtProvider(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public Token GenerateToken(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            return new Token {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        private RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.Now.AddMonths(1),
                CreatedDate = DateTime.Now,
                Enabled = true
            };

            return refreshToken;
        }

        public string GenerateAccessToken(User user)
        {
            string secretKey = _options.SecretKey;
            var securiryKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securiryKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("FirstName", user.FirstName.ToString()),
                    new Claim("SecondName", user.SecondName.ToString()),
                    new Claim("Id", user.Id.ToString())
                    ]),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = credentials,
                Issuer = _options.Issuer,
                Audience = _options.Audience
            };
            
            return new JsonWebTokenHandler().CreateToken(tokenDescriptor);
        }
    }

    public class Token
    {
        public string AccessToken { get; set; } = string.Empty;

        public required RefreshToken RefreshToken { get; set; }
    }
}
