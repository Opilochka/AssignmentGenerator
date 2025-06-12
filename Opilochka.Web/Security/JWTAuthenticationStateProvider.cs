using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using Opilochka.Web.Services.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Opilochka.Web.Security
{
    public class JWTAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly AccessTokenService _accessTokenService;
        private readonly ILogger<AccessTokenService> _logger;

        public JWTAuthenticationStateProvider(AccessTokenService accessTokenService, ILogger<AccessTokenService> logger)
        {
            _accessTokenService = accessTokenService;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _accessTokenService.GetToken();
                if (string.IsNullOrWhiteSpace(token))
                    return await MarkAsUnauthorize();

                var readJWT = new JwtSecurityTokenHandler().ReadJwtToken(token);
                var identity = new ClaimsIdentity(readJWT.Claims, "JWT");
                var principal = new ClaimsPrincipal(identity);
                return await Task.FromResult(new AuthenticationState(principal));
            }
            catch (SecurityTokenException tokenEx)
            {
                _logger.LogError("Ошибка токена: {Message}", tokenEx.Message);
                return await MarkAsUnauthorize();
            }
            catch (Exception ex)
            {
                _logger.LogError("Общая ошибка: {Message}", ex.Message);
                return await MarkAsUnauthorize();
            }
        }

        private Task<AuthenticationState> MarkAsUnauthorize()
        {
            try
            {
                var state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                NotifyAuthenticationStateChanged(Task.FromResult(state));
                return Task.FromResult(state);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при уведомлении об изменении состояния аутентификации: {Message}", ex.Message);
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
        }
    }
}
