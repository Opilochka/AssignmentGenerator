using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Opilochka.Web.Security
{
    public class JWTAuthenticationHandler : AuthenticationHandler<CustomOption>
    {
        private const string AccessTokenCookie = "access_token";
        private TimeProvider CustomTimeProvider { get; }
        public JWTAuthenticationHandler(
            IOptionsMonitor<CustomOption> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            TimeProvider timeProvider) : base(options, logger, encoder)
        {
            CustomTimeProvider = timeProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var token = Request.Cookies[AccessTokenCookie];
                    if (string.IsNullOrEmpty(token))
                    {
                        return AuthenticateResult.NoResult();
                    }

                    var jwtHandler = new JwtSecurityTokenHandler();
                    var readJWT = jwtHandler.ReadJwtToken(token);

                    if (readJWT.ValidTo < CustomTimeProvider.GetUtcNow())
                    {
                        Logger.LogWarning("JWT токен истек");
                        return AuthenticateResult.NoResult();
                    }

                    var identity = new ClaimsIdentity(readJWT.Claims, "JWT");
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);
                    return AuthenticateResult.Success(ticket);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Ошибка при обработке JWT токена: {Message}", ex.Message);
                    return AuthenticateResult.NoResult();
                }
            });  
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Redirect("/login");
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.Redirect("/accessdenied");
            return Task.CompletedTask;
        }
    }

    public class CustomOption : AuthenticationSchemeOptions
    {

    }
}
